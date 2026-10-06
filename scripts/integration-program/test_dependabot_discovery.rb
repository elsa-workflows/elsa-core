require "fileutils"
require "find"
require "minitest/autorun"
require "open3"
require "pathname"
require "tmpdir"
require "yaml"

class DependabotDiscoveryTests < Minitest::Test
  REPOSITORY_ROOT = File.expand_path("../..", __dir__)
  CONFIGURATION = YAML.load_file(File.join(REPOSITORY_ROOT, ".github/dependabot.yml"))
  LEGACY_CORE_PROJECT = "src/extensions/Elsa.Testing.Extensions/Elsa.Testing.Extensions.csproj"

  def setup
    @workspace = Dir.mktmpdir("dependabot-discovery-")
  end

  def teardown
    FileUtils.remove_entry(@workspace)
  end

  def test_terminal_double_star_does_not_discover_nested_projects
    add_project("src/extensions/communication/Elsa.Slack/Elsa.Slack.csproj")

    assert_empty discovered_projects(["/src/extensions/**"])
    assert_empty discovered_projects(["/src/extensions/**"], native: false)
  end

  def test_recursive_selector_discovers_direct_and_deeper_projects
    projects = [
      "src/extensions/Elsa.Direct/Elsa.Direct.csproj",
      "src/extensions/communication/Elsa.Slack/Elsa.Slack.csproj",
      "src/extensions/communication/deeper/Elsa.NewConnector/Elsa.NewConnector.csproj"
    ]
    projects.each { |project| add_project(project) }
    add_project("src/studio/modules/Other/Other.csproj")

    assert_equal projects.sort, discovered_projects(["/src/extensions/**/Elsa.*"])
    assert_equal projects.sort, discovered_projects(["/src/extensions/**/Elsa.*"], native: false)
    assert_equal projects.sort, discovered_projects(["/src/extensions/**/*"])
    refute_equal projects.sort, discovered_projects(["/src/extensions/*/*"])
  end

  def test_configured_selectors_cover_every_tracked_product_project
    output, status = Open3.capture2("git", "ls-files", "-z", "src/extensions", "src/studio", chdir: REPOSITORY_ROOT)
    assert status.success?, "Could not enumerate tracked product files"
    files = output.split("\0")
    files.each { |path| FileUtils.mkdir_p(File.dirname(File.join(@workspace, path))) }
    projects = files.select { |path| path.end_with?(".csproj") }
    projects.each { |project| add_project(project) }

    %w[extensions studio].each do |product|
      expected = projects.select { |path| path.start_with?("src/#{product}/") }.sort
      refute_empty expected
      patterns = product_entry(product).fetch("directories")
      assert_equal expected, discovered_projects(patterns)
      assert_equal expected, discovered_projects(patterns, native: false)
      assert_equal expected.map { |path| File.dirname(path) }.uniq.sort,
        patterns.flat_map { |pattern| native_matching_directories(pattern) }.uniq.sort,
        "Selectors should reach every project directory without scanning empty source directories"
    end

    solution = File.read(File.join(REPOSITORY_ROOT, "Elsa.sln")).scan(/"([^"\n]+\.csproj)"/).flatten.map { |path| path.tr("\\", "/") }
    assert_equal [LEGACY_CORE_PROJECT], (projects - solution).sort,
      "Reconcile new non-solution projects explicitly; do not silently omit them from discovery"
  end

  def test_existing_update_policies_are_preserved
    updates = CONFIGURATION.fetch("updates")
    assert_equal 3, updates.length
    root = updates.find { |entry| entry["directory"] == "/" }
    assert_equal "nuget", root.fetch("package-ecosystem")
    assert_equal({ "interval" => "weekly" }, root.fetch("schedule"))
    assert_equal %w[elsa-feedz-preview valence-loom-feedz], root.fetch("registries")
    assert_equal %w[src/extensions/** src/studio/**], root.fetch("exclude-paths")

    %w[extensions studio].each do |product|
      entry = product_entry(product)
      assert_equal ["/src/#{product}/**/Elsa.*"], entry.fetch("directories")
      refute entry.key?("directory")
      assert_equal "nuget", entry.fetch("package-ecosystem")
      assert_equal "main", entry.fetch("target-branch")
      assert_equal ["elsa-feedz-preview"], entry.fetch("registries")
      assert_equal({ "interval" => "daily", "time" => "06:00", "timezone" => "Europe/Amsterdam" }, entry.fetch("schedule"))
      assert_equal 1, entry.fetch("open-pull-requests-limit")
      assert_equal({ "prefix" => "deps", "include" => "scope" }, entry.fetch("commit-message"))
    end
    assert_equal({ "elsa-core-preview" => { "patterns" => ["Elsa*"] } }, product_entry("extensions").fetch("groups"))
    refute product_entry("studio").key?("groups")
  end

  private

  def product_entry(product)
    CONFIGURATION.fetch("updates").find do |entry|
      entry.fetch("directories", []).any? { |pattern| pattern.start_with?("/src/#{product}/") }
    end
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
      # NuGet DiscoveryWorker.FindEntryPoints enumerates only this directory.
      directories.flat_map do |directory|
        Dir.children(directory).select { |name| name.end_with?(".csproj") }.map do |name|
          Pathname.new(File.join(directory, name)).cleanpath.to_s
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
    matcher = Regexp.new(expression, Regexp::IGNORECASE)
    Find.find(@workspace).select { |path| path != @workspace && File.directory?(path) }
      .map { |path| Pathname.new(path).relative_path_from(Pathname.new(@workspace)).to_s }
      .select { |directory| matcher.match?(directory) }.sort
  end
end
