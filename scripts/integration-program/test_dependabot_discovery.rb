require "fileutils"
require "find"
require "minitest/autorun"
require "open3"
require "pathname"
require "rexml/document"
require "tmpdir"
require "yaml"

class DependabotDiscoveryTests < Minitest::Test
  REPOSITORY_ROOT = File.expand_path("../..", __dir__)
  CONFIGURATION = YAML.load_file(File.join(REPOSITORY_ROOT, ".github/dependabot.yml"))
  LEGACY_CORE_PROJECT = "extensions/src/Elsa.Testing.Extensions/Elsa.Testing.Extensions.csproj"

  def setup
    @workspace = Dir.mktmpdir("dependabot-discovery-")
  end

  def teardown
    FileUtils.remove_entry(@workspace)
  end

  def test_terminal_double_star_does_not_discover_nested_projects
    add_project("extensions/src/communication/Elsa.Slack/Elsa.Slack.csproj")

    assert_empty discovered_projects(["/extensions/src/**"])
    assert_empty discovered_projects(["/extensions/src/**"], native: false)
  end

  def test_recursive_selector_discovers_direct_and_deeper_projects
    projects = [
      "extensions/src/Elsa.Direct/Elsa.Direct.csproj",
      "extensions/src/communication/Elsa.Slack/Elsa.Slack.csproj",
      "extensions/src/communication/deeper/Elsa.NewConnector/Elsa.NewConnector.csproj"
    ]
    projects.each { |project| add_project(project) }
    add_project("studio/src/modules/Other/Other.csproj")

    assert_equal projects.sort, discovered_projects(["/extensions/src/**/Elsa.*"])
    assert_equal projects.sort, discovered_projects(["/extensions/src/**/Elsa.*"], native: false)
    assert_equal projects.sort, discovered_projects(["/extensions/src/**/*"])
    refute_equal projects.sort, discovered_projects(["/extensions/src/*/*"])
  end

  def test_solution_entry_covers_every_product_project_and_shared_build
    projects = tracked_projects
    projects.each { |project| add_project(project) }
    FileUtils.cp(File.join(REPOSITORY_ROOT, "Elsa.sln"), @workspace)
    expected = solution_projects(File.join(REPOSITORY_ROOT, "Elsa.sln")) + [LEGACY_CORE_PROJECT]

    assert_equal expected.sort, discovered_projects(update_entry.fetch("directories"))
    %w[core extensions studio].each do |product|
      assert_equal ["core/test/TlsSmoke/TlsSmoke.csproj"].select { |path| path.start_with?("#{product}/") },
        projects.select { |path| path.start_with?("#{product}/") } - expected,
        "Every product source, test and sample project needs an explicit disposition"
    end
    assert_includes expected, "build/_build.csproj"
    # These are version-pinned historical/contract probes, not product update inputs.
    # The complete list is reconciled in the discovery documentation under #8636.
    documented_probes = File.read(File.join(REPOSITORY_ROOT,
      "docs/integration-program/consolidation/dependabot-recursive-discovery.md"))
      .scan(/`(scripts\/[^`]+\.csproj)`/).flatten.sort
    assert_equal projects.grep(%r{^scripts/}).sort, documented_probes
    assert_equal ["core/test/TlsSmoke/TlsSmoke.csproj"] + documented_probes,
      (projects - expected).sort
    assert_empty expected - projects, "Solution entries must name tracked manifests"
  end

  def test_solution_expansion_includes_tests_samples_and_deeper_projects
    projects = %w[
      extensions/src/deeper/Connector/Connector.csproj
      extensions/test/Connector.Tests/Connector.Tests.csproj
      studio/samples/Host/Host.csproj
    ]
    (projects + [LEGACY_CORE_PROJECT]).each { |project| add_project(project) }
    File.write(File.join(@workspace, "Elsa.sln"), projects.map do |project|
      %(Project("{TYPE}") = "Fixture", "#{project.tr("/", "\\")}", "{ID}"\nEndProject\n)
    end.join)
    # The native entry-point scanner does not support solution filters.
    add_project("ignored/Other.csproj")
    File.write(File.join(@workspace, "Elsa.Other.slnf"), '{"solution":{"path":"ignored/Other.csproj"}}')

    assert_equal (projects + [LEGACY_CORE_PROJECT]).sort,
      discovered_projects(["/", "/extensions/src/Elsa.Testing.Extensions"])
  end

  def test_selected_projects_reconcile_central_package_imports
    selected = solution_projects(File.join(REPOSITORY_ROOT, "Elsa.sln")) + [LEGACY_CORE_PROJECT]
    central_versions = {}
    import_chains = {}
    selected.each do |project|
      expected = case project
      when %r{^extensions/} then "extensions/src/Directory.Packages.props"
      when %r{^studio/} then "studio/src/Directory.Packages.props"
      else "Directory.Packages.props"
      end
      central = nearest_central_file(project)
      chain = import_chains[central] ||= central_import_chain(central)
      assert_equal expected, chain.last, "#{project}: #{chain.join(' -> ')}"
      versions = central_versions[chain.last] ||= REXML::Document.new(File.read(File.join(REPOSITORY_ROOT, chain.last)))
        .get_elements("//PackageVersion")
      refute_empty versions, "#{project} must reach central package versions"
    end
    %w[extensions/test extensions/samples studio/test studio/samples].each do |directory|
      product = directory.split("/").first
      assert_equal ["#{directory}/Directory.Packages.props", "#{product}/src/Directory.Packages.props"],
        central_import_chain("#{directory}/Directory.Packages.props")
    end
  end

  def test_coordinated_update_policy_preserves_feeds_and_bounds_proposals
    assert_equal 1, CONFIGURATION.fetch("updates").length
    entry = update_entry
    assert_equal ["/", "/extensions/src/Elsa.Testing.Extensions"], entry.fetch("directories")
    refute entry.key?("directory")
    refute entry.key?("exclude-paths")
    assert_equal "nuget", entry.fetch("package-ecosystem")
    assert_equal "main", entry.fetch("target-branch")
    assert_equal %w[elsa-feedz-preview valence-loom-feedz], entry.fetch("registries")
    assert_equal({ "interval" => "daily", "time" => "06:00", "timezone" => "Europe/Amsterdam" }, entry.fetch("schedule"))
    assert_equal 1, entry.fetch("open-pull-requests-limit")
    assert_equal({ "elsa-core-preview" => { "patterns" => ["Elsa*"] } }, entry.fetch("groups"))
    assert_equal({ "prefix" => "deps", "include" => "scope" }, entry.fetch("commit-message"))
    assert_equal "${{secrets.FEEDZ_API_KEY}}", CONFIGURATION.fetch("registries").fetch("elsa-feedz-preview").fetch("token")
    assert_equal "https://f.feedz.io/valence-works/loom/nuget/index.json",
      CONFIGURATION.fetch("registries").fetch("valence-loom-feedz").fetch("url")
  end

  def test_coverage_workflow_runs_when_discovery_inputs_change
    workflow = YAML.load_file(File.join(REPOSITORY_ROOT, ".github/workflows/integration-program-tools.yml"))
    paths = workflow.fetch("on") { workflow.fetch(true) }.fetch("pull_request").fetch("paths")
    inputs = tracked_projects + %w[
      Elsa.sln Directory.Packages.props NuGet.Config nuget.config core/custom.props studio/import.targets
      core/global.json docs/integration-program/consolidation/dependabot-recursive-discovery.md
    ] +
      Dir.chdir(REPOSITORY_ROOT) { Dir.glob("**/*.{props,targets}") }
    inputs.each do |path|
      assert paths.any? { |pattern| File.fnmatch?(pattern, path, File::FNM_PATHNAME) },
        "Discovery input #{path} must trigger the coverage gate"
    end
  end

  private

  def update_entry
    CONFIGURATION.fetch("updates").find { |entry| entry.fetch("directories", []).include?("/") }
  end

  def tracked_projects
    output, status = Open3.capture2("git", "ls-files", "-z", chdir: REPOSITORY_ROOT)
    assert status.success?, "Could not enumerate tracked files"
    output.split("\0").select { |path| path.end_with?(".csproj") }.sort
  end

  def solution_projects(path)
    File.read(path, encoding: "UTF-8").scan(/"([^"\n]+\.csproj)"/).flatten.map { |project| project.tr("\\", "/") }
  end

  def nearest_central_file(project)
    directory = Pathname.new(project).dirname
    loop do
      path = directory.join("Directory.Packages.props").cleanpath.to_s
      return path if File.file?(File.join(REPOSITORY_ROOT, path))
      raise "No central package file for #{project}" if directory.to_s == "."
      directory = directory.parent
    end
  end

  def central_import_chain(path, visited = [])
    raise "Central package import cycle: #{path}" if visited.include?(path)
    document = REXML::Document.new(File.read(File.join(REPOSITORY_ROOT, path)))
    imports = document.get_elements("//Import")
    return [path] if imports.empty?
    assert_equal 1, imports.length, "Reconcile changed central import topology: #{path}"
    imported = Pathname.new(File.dirname(path)).join(imports.first.attributes["Project"]).cleanpath.to_s
    [path] + central_import_chain(imported, visited + [path])
  end

  def add_project(path)
    absolute_path = File.join(@workspace, path)
    FileUtils.mkdir_p(File.dirname(absolute_path))
    File.write(absolute_path, "<Project />")
  end

  def discovered_projects(patterns, native: true)
    Dir.chdir(@workspace) do
      # Model the hosted NuGet PathHelper matcher first. The optional Ruby branch
      # corroborates it with FileFetcherCommand.files_from_multidirectories at
      # updater 9c06d60057ba9e7e79210e6618f932a28cf6a158.
      directories = patterns.flat_map do |pattern|
        if native
          native_matching_directories(pattern)
        else
          Dir.glob(pattern.delete_prefix("/"), File::FNM_DOTMATCH).select { |directory| File.directory?(directory) }
        end
      end.uniq
      # NuGet enumerates direct entry points and expands projects from solutions.
      directories.flat_map do |directory|
        Dir.children(directory).flat_map do |name|
          path = File.join(directory, name)
          if name.end_with?(".sln")
            solution_projects(path).map { |project| Pathname.new(File.join(directory, project)).cleanpath.to_s }
          elsif name.end_with?(".csproj")
            [Pathname.new(path).cleanpath.to_s]
          else
            []
          end
        end
      end.uniq.sort
    end
  end

  def native_matching_directories(pattern)
    # Port of PathHelper.GetMatchingDirectoriesUnder at the hosted updater SHA
    # above: **/ consumes its separator, terminal ** is two single-level stars,
    # and a trailing /**/* includes the named directory and all descendants.
    relative_pattern = pattern.tr("\\", "/").sub(%r{^/+}, "").sub(%r{/+$}, "")
    expression = "^"
    index = 0
    append_anchor = true
    while index < relative_pattern.length
      remaining = relative_pattern[index..-1]
      if remaining == "/**/*"
        expression += "($|/.*$)"
        append_anchor = false
        break
      end
      character = relative_pattern[index]
      if character == "*" && remaining.start_with?("**/")
        expression += ".*"
        index += 3
        next
      end
      expression += case character
      when "*" then "[^/]*"
      when "?" then "."
      when "/" then "/"
      else Regexp.escape(character)
      end
      index += 1
    end
    expression += "$" if append_anchor
    return ["."] if relative_pattern.empty?
    matcher = Regexp.new(expression, Regexp::IGNORECASE)
    Find.find(@workspace).select { |path| path != @workspace && File.directory?(path) }
      .map { |path| Pathname.new(path).relative_path_from(Pathname.new(@workspace)).to_s }
      .select { |directory| matcher.match?(directory) }.sort
  end
end
