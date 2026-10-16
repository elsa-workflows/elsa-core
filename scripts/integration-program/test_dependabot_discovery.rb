require "fileutils"
require "find"
require "json"
require "minitest/autorun"
require "open3"
require "pathname"
require "rexml/document"
require "tmpdir"
require "yaml"

class DependabotDiscoveryTests < Minitest::Test
  REPOSITORY_ROOT = File.expand_path("../..", __dir__)
  CONFIGURATION = YAML.load_file(File.join(REPOSITORY_ROOT, ".github/dependabot.yml"))
  UNMODELED_PROJECT_EXTENSIONS = %w[.proj .fsproj .vbproj].freeze
  APPS_PROJECTS = JSON.parse(File.read(File.join(REPOSITORY_ROOT,
    "Elsa.Apps.slnf"))).fetch("solution").fetch("projects").sort.freeze
  TEMPLATES = JSON.parse(File.read(File.join(REPOSITORY_ROOT,
    "scripts/solution/solution-groups.json"))).fetch("independentProducts").fetch("templates").freeze
  TEMPLATES_PROJECTS = (TEMPLATES.fetch("projects") + TEMPLATES.fetch("buildData")).sort.freeze
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

  def test_batches_partition_the_complete_selected_inventory
    projects = tracked_projects
    projects.each { |project| add_project(project) }
    assert_empty tracked_files.select { |path| UNMODELED_PROJECT_EXTENSIONS.include?(File.extname(path).downcase) },
      "Reconcile the native entry-point model before adding .proj, .fsproj or .vbproj files"
    tracked_files.select { |path| %w[.sln .slnx].include?(File.extname(path).downcase) }.each do |path|
      destination = File.join(@workspace, path)
      FileUtils.mkdir_p(File.dirname(destination))
      FileUtils.cp(File.join(REPOSITORY_ROOT, path), destination)
    end
    expected = solution_projects(File.join(REPOSITORY_ROOT, "Elsa.sln")) - APPS_PROJECTS + [LEGACY_CORE_PROJECT]

    batches = CONFIGURATION.fetch("updates").map { |entry| discovered_projects(entry.fetch("directories")) }
    assert_equal expected.sort, batches.flatten.sort, "Reconcile batch selectors when solution membership changes"
    assert_equal batches.flatten.length, batches.flatten.uniq.length, "Project starting sets must not overlap"
    assert_equal [114, 69, 100, 73, 1], batches.map(&:length)
    assert_equal 173, batches.flatten.grep(%r{^(extensions|studio)/}).length
    assert_includes batches[2], LEGACY_CORE_PROJECT
    CONFIGURATION.fetch("updates").each do |entry|
      refute_includes entry.fetch("directories"), "/", "Never select the root solution as a batch"
    end
    %w[core extensions studio apps templates].each do |product|
      unselected = case product
      when "apps" then APPS_PROJECTS
      when "templates" then TEMPLATES_PROJECTS
      else ["core/test/TlsSmoke/TlsSmoke.csproj"].select { |path| path.start_with?("#{product}/") }
      end
      assert_equal unselected,
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
    assert_equal (APPS_PROJECTS + TEMPLATES_PROJECTS + ["core/test/TlsSmoke/TlsSmoke.csproj"] + documented_probes).sort,
      (projects - expected).sort
    assert_empty expected - projects, "Solution entries must name tracked manifests"
  end

  def test_apps_have_an_explicit_independent_consumer_disposition
    refute_empty APPS_PROJECTS
    assert_equal APPS_PROJECTS, tracked_projects.grep(%r{^apps/}).sort
    assert_equal APPS_PROJECTS.length, APPS_PROJECTS.uniq.length
    assert_equal APPS_PROJECTS, solution_projects(File.join(REPOSITORY_ROOT, "Elsa.sln")).grep(%r{^apps/}).sort
    assert APPS_PROJECTS.all? { |path| path.start_with?("apps/") }
    targets = project_xml("apps/Directory.Build.targets")
    assert_equal ["false"], targets.get_elements("//UseProjectReferences").map(&:text)
    assert_equal ["false"], targets.get_elements("//IsPackable").map(&:text)
    assert_equal ["$(MSBuildThisFileDirectory)NuGet.Config"], targets.get_elements("//RestoreConfigFile").map(&:text)
    assert_empty targets.get_elements("//Import"), "Apps must not import root source/pack policy"
    assert_includes File.read(File.join(REPOSITORY_ROOT,
      "docs/integration-program/consolidation/dependabot-recursive-discovery.md")),
      "independent released-package consumers"
  end

  def test_templates_have_an_explicit_independent_build_and_data_disposition
    assert_equal TEMPLATES_PROJECTS, tracked_projects.grep(%r{^templates/}).sort
    assert_equal TEMPLATES_PROJECTS.length, TEMPLATES_PROJECTS.uniq.length
    assert_empty solution_projects(File.join(REPOSITORY_ROOT, "Elsa.sln")).grep(%r{^templates/})
    product_projects = solution_projects(File.join(REPOSITORY_ROOT, TEMPLATES.fetch("solution")))
      .map { |path| "templates/#{path}" }.sort
    assert_equal TEMPLATES.fetch("projects").sort, product_projects
    assert_empty product_projects & TEMPLATES.fetch("buildData")
    filtered = JSON.parse(File.read(File.join(REPOSITORY_ROOT, TEMPLATES.fetch("filter")))).fetch("solution")
    assert_equal TEMPLATES.fetch("solution"), filtered.fetch("path")
    assert_equal product_projects, filtered.fetch("projects").map { |path| "templates/#{path}" }.sort
    targets = project_xml("templates/Directory.Build.targets")
    assert_equal ["false"], targets.get_elements("//UseProjectReferences").map(&:text)
    assert_empty targets.get_elements("//Import"), "Templates must not import Core source/pack targets"
    assert_empty targets.get_elements("//IsPackable"), "Keep the independent template pack packable"
    assert_equal ["$(MSBuildThisFileDirectory)NuGet.Config"], targets.get_elements("//RestoreConfigFile").map(&:text)
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

  def test_slnx_entry_expansion_uses_paths_relative_to_the_solution_directory
    root_project = "core/src/RootSlnxFixture.csproj"
    nested_project = "extensions/src/Elsa.Testing.Extensions/deeper/Fixture.csproj"
    add_project(root_project)
    add_project(nested_project)

    File.write(File.join(@workspace, "Elsa.slnx"), <<~XML)
      <Solution>
        <Project Path="#{root_project}" />
      </Solution>
    XML

    nested_directory = File.join(@workspace, "extensions/src/Elsa.Testing.Extensions")
    File.write(File.join(nested_directory, "Elsa.Testing.Extensions.slnx"), <<~XML)
      <Solution>
        <Project Path="deeper/Fixture.csproj" />
      </Solution>
    XML

    assert_equal [root_project, nested_project].sort,
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
      when %r{^apps/} then "apps/Directory.Packages.props"
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

  def test_static_project_reference_and_build_import_closure_stays_available
    selected = solution_projects(File.join(REPOSITORY_ROOT, "Elsa.sln")) + [LEGACY_CORE_PROJECT]
    closure = selected.flat_map { |project| static_source_closure(project) }.uniq
    references = closure.flat_map do |path|
      project_xml(path).get_elements("//ProjectReference").map do |reference|
        resolve_source_path(path, reference.attributes.fetch("Include").value)
      end
    end.uniq
    assert_empty references - selected, "Referenced projects need an explicit update disposition"
    %w[Directory.Build.props core/src/Directory.Build.props extensions/src/Directory.Build.props
      studio/src/Directory.Build.props build/ConsolidatedPackageProof.targets].each do |path|
      assert_includes closure, path
    end
    %w[core extensions studio].each do |product|
      frameworks = project_xml("#{product}/src/Directory.Build.props").get_elements("//TargetFrameworks").map(&:text)
      assert_includes frameworks, "net8.0;net9.0;net10.0", "Do not narrow the product source frameworks"
    end
  end

  def test_batch_policy_preserves_feeds_and_makes_per_job_caps_explicit
    entries = CONFIGURATION.fetch("updates")
    assert_equal 5, entries.length
    assert_equal [
      ["/core/src/**/*"], ["/core/test/**/Elsa.*"], ["/extensions/**/*"],
      ["/studio/**/*"], ["/build", "/.config"]
    ], entries.map { |entry| entry.fetch("directories") }
    entries.zip(%w[06:00 09:00 12:00 15:00 18:00]).each do |entry, time|
      refute entry.key?("directory")
      refute entry.key?("exclude-paths")
      assert_equal "nuget", entry.fetch("package-ecosystem")
      assert_equal "main", entry.fetch("target-branch")
      assert_equal %w[elsa-feedz-preview valence-loom-feedz], entry.fetch("registries")
      assert_equal({ "interval" => "daily", "time" => time, "timezone" => "Europe/Amsterdam" }, entry.fetch("schedule"))
      assert_equal 1, entry.fetch("open-pull-requests-limit")
      assert_equal({ "elsa-core-preview" => { "patterns" => ["Elsa*"] } }, entry.fetch("groups"))
      assert_equal({ "prefix" => "deps", "include" => "scope" }, entry.fetch("commit-message"))
    end
    assert_equal 5, entries.sum { |entry| entry.fetch("open-pull-requests-limit") }
    assert_equal "${{secrets.FEEDZ_API_KEY}}", CONFIGURATION.fetch("registries").fetch("elsa-feedz-preview").fetch("token")
    assert_equal "https://f.feedz.io/valence-works/loom/nuget/index.json",
      CONFIGURATION.fetch("registries").fetch("valence-loom-feedz").fetch("url")
  end

  def test_future_deeper_projects_stay_visible_and_tls_probe_stays_outside
    projects = %w[
      core/src/future/deeper/Arbitrary/Arbitrary.csproj
      core/test/unit/deeper/Elsa.Future.Tests/Elsa.Future.Tests.csproj
      extensions/samples/deeper/Arbitrary/Arbitrary.csproj
      studio/test/deeper/Arbitrary/Arbitrary.csproj
      build/_build.csproj
    ]
    (projects + ["core/test/TlsSmoke/TlsSmoke.csproj"]).each { |project| add_project(project) }
    File.write(File.join(@workspace, "Elsa.sln"), 'invalid solution must never be opened')
    actual = CONFIGURATION.fetch("updates").flat_map { |entry| discovered_projects(entry.fetch("directories")) }
    assert_equal projects.sort, actual.sort
    add_project("core/test/unit/deeper/Other.Tests/Other.Tests.csproj")
    refute_includes discovered_projects(["/core/test/**/Elsa.*"]),
      "core/test/unit/deeper/Other.Tests/Other.Tests.csproj",
      "A differently named solution test requires selector reconciliation"
  end

  def test_nested_solutions_expose_cross_batch_starting_project_overlap
    core_project = "core/src/deeper/EntryPoint/EntryPoint.csproj"
    studio_project = "studio/src/deeper/Other/Other.csproj"
    [core_project, studio_project].each { |project| add_project(project) }
    directory = File.dirname(core_project)
    relative_studio = Pathname.new(studio_project).relative_path_from(Pathname.new(directory)).to_s
    { ".sln" => %(Project("{TYPE}") = "Other", "#{relative_studio}", "{ID}"\nEndProject\n),
      ".slnx" => %(<Solution><Project Path="#{relative_studio}" /></Solution>) }.each do |extension, contents|
      solution = File.join(@workspace, directory, "Nested#{extension}")
      File.write(solution, contents)
      core_batch = discovered_projects(["/core/src/**/*"])
      studio_batch = discovered_projects(["/studio/**/*"])
      assert_equal [studio_project], core_batch & studio_batch,
        "A nested #{extension} must expose overlapping project starts to the partition gate"
      File.delete(solution)
    end
  end

  def test_unmodeled_native_entrypoints_fail_closed
    UNMODELED_PROJECT_EXTENSIONS.each do |extension|
      path = "core/src/Future/Unsupported#{extension}"
      add_project(path)
      error = assert_raises(RuntimeError) { discovered_projects(["/core/src/**/*"]) }
      assert_includes error.message, "Reconcile native entry-point model"
      File.delete(File.join(@workspace, path))
    end
  end

  def test_explicit_tool_manifest_entry_preserves_root_tool
    tool_directory = File.join(@workspace, ".config")
    FileUtils.mkdir_p(tool_directory)
    FileUtils.cp(File.join(REPOSITORY_ROOT, ".config/dotnet-tools.json"), tool_directory)
    entry = CONFIGURATION.fetch("updates").last
    assert_includes entry.fetch("directories"), "/.config"
    assert_equal [".config"], native_matching_directories("/.config")
    tools = JSON.parse(File.read(File.join(tool_directory, "dotnet-tools.json"))).fetch("tools")
    refute_empty tools.fetch("dotnet-ef").fetch("version"), "Keep the root tool discoverable as its version changes"
  end

  def test_coverage_workflow_runs_when_discovery_inputs_change
    workflow = YAML.load_file(File.join(REPOSITORY_ROOT, ".github/workflows/integration-program-tools.yml"))
    paths = workflow.fetch("on") { workflow.fetch(true) }.fetch("pull_request").fetch("paths")
    inputs = tracked_projects + %w[
      Elsa.Future.sln Elsa.Future.slnx Future.proj Future.fsproj Future.vbproj
      extensions/src/Elsa.Testing.Extensions/Future.sln extensions/src/Elsa.Testing.Extensions/Future.slnx
      extensions/src/Elsa.Testing.Extensions/Future.cs
      Directory.Packages.props NuGet.Config nuget.config core/custom.props studio/import.targets
      .config/dotnet-tools.json
      core/global.json docs/integration-program/consolidation/dependabot-recursive-discovery.md
      Elsa.Apps.slnf Elsa.Templates.slnf scripts/solution/solution-groups.json
    ] +
      Dir.chdir(REPOSITORY_ROOT) { Dir.glob("**/*.{sln,slnx,props,targets}") }
    future_solutions = {
      "Elsa.Future.slnx" => ["**/*.slnx"],
      "extensions/src/Elsa.Testing.Extensions/Future.slnx" => [
        "**/*.slnx", "extensions/src/Elsa.Testing.Extensions/**"
      ]
    }
    future_solutions.each do |path, expected_patterns|
      assert_equal expected_patterns,
        paths.select { |pattern| File.fnmatch?(pattern, path, File::FNM_PATHNAME) }
    end
    inputs.each do |path|
      assert paths.any? { |pattern| File.fnmatch?(pattern, path, File::FNM_PATHNAME) },
        "Discovery input #{path} must trigger the coverage gate"
    end
  end

  private

  def tracked_files
    @tracked_files ||= begin
      output, status = Open3.capture2("git", "ls-files", "-z", chdir: REPOSITORY_ROOT)
      assert status.success?, "Could not enumerate tracked files"
      output.split("\0").sort
    end
  end

  def tracked_projects
    tracked_files.select { |path| path.end_with?(".csproj") }
  end

  def solution_projects(path)
    if path.end_with?(".slnx")
      document = REXML::Document.new(File.read(path, encoding: "UTF-8"))
      document.get_elements("//Project").map { |project| project.attributes.fetch("Path").value.tr("\\", "/") }
    else
      File.read(path, encoding: "UTF-8").scan(/"([^"\n]+\.csproj)"/).flatten.map { |project| project.tr("\\", "/") }
    end
  end

  def project_xml(path)
    @source_documents ||= {}
    @source_documents[path] ||= REXML::Document.new(File.read(File.join(REPOSITORY_ROOT, path)))
  end

  def nearest_source_file(path, name)
    directory = Pathname.new(path).dirname
    loop do
      candidate = directory.join(name).cleanpath.to_s
      return candidate if File.file?(File.join(REPOSITORY_ROOT, candidate))
      return nil if directory.to_s == "."
      directory = directory.parent
    end
  end

  def resolve_source_path(path, value)
    # Resolve only the two repository import forms; new expressions fail closed.
    if value.start_with?("$([MSBuild]::GetPathOfFileAbove(")
      match = value.match(/GetPathOfFileAbove\('([^']+)', '\$\(MSBuildThisFileDirectory\)([^']+)'\)/)
      refute_nil match, "Reconcile MSBuild import expression in #{path}: #{value}"
      base = Pathname.new(File.dirname(path)).join(match[2]).cleanpath.join("placeholder").to_s
      resolved = nearest_source_file(base, match[1])
      refute_nil resolved, "Missing ancestor import for #{path}"
      return resolved
    end
    value = value.gsub("$(MSBuildThisFileDirectory)", "")
    refute_match(/[$*?]/, value, "Reconcile dynamic source path in #{path}: #{value}")
    resolved = Pathname.new(File.dirname(path)).join(value.tr("\\", "/")).cleanpath.to_s
    assert File.file?(File.join(REPOSITORY_ROOT, resolved)), "Missing source path #{resolved} from #{path}"
    resolved
  end

  def static_source_closure(project)
    pending = [project] + %w[Directory.Build.props Directory.Build.targets Directory.Packages.props].map do |name|
      nearest_source_file(project, name)
    end.compact
    seen = []
    until pending.empty?
      path = pending.pop
      next if seen.include?(path)
      seen << path
      pending.concat(project_xml(path).get_elements("//Import").map do |import|
        resolve_source_path(path, import.attributes.fetch("Project").value)
      end)
    end
    seen
  end

  def nearest_central_file(project)
    nearest_source_file(project, "Directory.Packages.props") || raise("No central package file for #{project}")
  end

  def central_import_chain(path, visited = [])
    raise "Central package import cycle: #{path}" if visited.include?(path)
    document = project_xml(path)
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
      # source d3bc6c74cbd106d79d24a728ab623a3eaa776087.
      # This does not establish executed helper binary equivalence.
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
          if UNMODELED_PROJECT_EXTENSIONS.include?(File.extname(name).downcase)
            raise "Reconcile native entry-point model: #{path}"
          elsif name.end_with?(".sln", ".slnx")
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
    # Port of PathHelper.GetMatchingDirectoriesUnder at source d3bc6c74cbd106d79d24a728ab623a3eaa776087
    # (Utilities/PathHelper.cs): **/ consumes its separator, terminal ** is two single-level stars,
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
