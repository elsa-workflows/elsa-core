# Contributing to Elsa Workflows
We love your input! We want to make contributing to this project as easy and transparent as possible, whether it's:

- Reporting a bug
- Discussing the current state of the code
- Submitting a fix
- Proposing new features
- Becoming a maintainer

## We develop with Github
We use github to host code, to track issues and feature requests, as well as to accept pull requests.

## We use [Github Flow](https://guides.github.com/introduction/flow/index.html), So all code changes happen through pull requests
Pull requests are the best way to propose changes to the codebase (we use [Github Flow](https://guides.github.com/introduction/flow/index.html)). We actively welcome your pull requests:

1. Fork the repo and create your branch from `master` or for Elsa 2.0 `feature/elsa-2.0`.
2. Issue that pull request!

### Sync Fork

To avoid conflicts, the fork should be regularly kept up to date.

First, the fork must be checked out locally `git clone https://github.com/elsa-workflows/elsa-core.git`. If this is already done, you only need to go to the project directory of the fork.

When you sync the local fork for the first time, you need to add the upstream from Elsa. 
`git remote add upstream https://github.com/elsa-workflows/elsa-core`

With the command `git remote -v` all existing upstreams can be read.

Before the changes can be applied, the changes must be fetched.
`git fetch upstream`

Afterwards the changes will be merged with your brach.
```git checkout <your_development_brach>
git merge upstream/master
```

Now all the changes from the upstream repository are in the local fork. Lastly, push the local changes to the remote repository on Github.
```# optional
git add .
git commit -m "fork synced with upstream"
# mandatory
git push origin master
```

## Any contributions you make will be under the New BSD License
In short, when you submit code changes, your submissions are understood to be under the same [New BSD License](https://github.com/elsa-workflows/elsa-core/blob/master/LICENSE) that covers the project. Feel free to contact the maintainers if that's a concern.

## Report bugs using Github's [issues](https://github.com/elsa-workflows/elsa-core/issues)
We use GitHub issues to track public bugs. Report a bug by [opening a new issue](https://github.com/elsa-workflows/elsa-core/issues/new); it's that easy!

## Write bug reports with detail, background, and sample code
**Great Bug Reports** tend to have:

- A quick summary and/or background
- Steps to reproduce
  - Be specific!
  - Give sample code if you can.
- What you expected would happen
- What actually happens
- Notes (possibly including why you think this might be happening, or stuff you tried that didn't work)

People *love* thorough bug reports. I'm not even kidding.

## Use a Consistent Coding Style
As a default, I am applying [Microsoft's Coding Conventions for .NET](https://docs.microsoft.com/en-us/visualstudio/ide/editorconfig-code-style-settings-reference).

## License
By contributing, you agree that your contributions will be licensed under its [New BSD License](https://github.com/elsa-workflows/elsa-core/blob/master/LICENSE).

## References
This document was adapted from the following [Gist](https://gist.github.com/briandk/3d2e8b3ec8daf5a27a62)
