# Changelogged

> [!NOTE]
> This tool is used in internal projects and any external support is not provided.

## Usage

### Adding Changelogs
Only PRs to the branches `master` and `dev` may have changelog sections.

Changelog sections must begin with a heading of any level. The changelog section must then contain one or more project headings which are exactly one level higher than the changelog heading. The project sections must contain exactly one list containing the changelog. The list items must be in the format `<change-type>: <change>` where `<change-type>` is `add`, `feat`, or `fix`. An example of a valid changelog is provided below.
```md
## Changelog

### Project
- add: Added this to that.
- feat: Changed this to that from those.
- fix: Fixed this.
```

### Creating Release PRs
> [!NOTE]
> The workflow does not automatically bump project versions, so it must be done manually *before* the release PR is made.

Release PRs may release one or more products. In the case of one product, the title format is `release: <Project Name>`; for example, `release: Project1`. In the case of multiple products, the title format is `release: <ProjectName>{,Project Names}`; for example, `release: Project1,Project2`. There must be no spaces before or after commas.

Release PRs may contain changelogs.

After the release PR is created, the workflow will automatically create draft release(s) for the products specified in the title. The contents of the draft release are the calculated changelogs for each project. Changelogs are sorted alphabetically. The changelog will continue to update alongside the PR until the PR is closed or merged.
