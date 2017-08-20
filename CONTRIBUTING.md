# Contribution Guide

We'd love for you to contribute to our source code and to make Fornax even better than it is
today! Here are the guidelines we'd like you to follow:

 - [Issues and Bugs](#issue)
 - [Feature Requests](#feature)
 - [Documentation fixes](#docs)
 - [Submission Guidelines](#submit)
 - [Coding Rules](#rules)
 - [Build Process](#build)

## <a name="issue"></a> Found an Issue?

If you find a bug in the source code or a mistake in the documentation, you can help us by
submitting an issue to our [GitLab Repository](https://gitlab.com/LambdaFactory/Fornax). Even better you can submit a Pull Request
with a fix.


## <a name="feature"></a> Want a Feature?

You can request a new feature by submitting an issue to our [GitLab Repository](https://gitlab.com/LambdaFactory/Fornax). If you would like to implement a new feature then consider what kind of change it is:

* **Major Changes** that you wish to contribute to the project should be discussed first on in the issue so that we can better coordinate our efforts, prevent duplication of work, and help you to craft the change so that it is successfully accepted into the project.

* **Small Changes** can be crafted and submitted to the [GitLab Repository](https://gitlab.com/LambdaFactory/Fornax) as a Pull Request.

## <a name="docs"></a> Want a Doc Fix?

If you want to help improve the docs, it's a good idea to let others know what you're working on to
minimize duplication of effort. Create a new issue (or comment on a related existing one) to let
others know what you're working on.

For large fixes, please build and test the documentation before submitting the PR to be sure you
haven't accidentally introduced any layout or formatting issues.

## <a name="submit"></a> Submission Guidelines

### Submitting an Issue
Before you submit your issue search the archive, maybe your question was already answered.

If your issue appears to be a bug, and hasn't been reported, open a new issue. Help us to maximize
the effort we can spend fixing issues and adding new features, by not reporting duplicate issues.
Providing the following information will increase the chances of your issue being dealt with
quickly:

* **Overview of the Issue** - if an error is being thrown a non-minified stack trace helps
* **Motivation for or Use Case** - explain why this is a bug for you
* **Fornax Version(s)** - is it a regression?
* **Environment and Operating System** - installed .Net / mono version, OS
* **Reproduce the Error** - provide set of steps to reproduce error
* **Related Issues** - has a similar issue been reported before?
* **Suggest a Fix** - if you can't fix the bug yourself, perhaps you can point to what might be
  causing the problem (line of code or commit)

### Submitting a Pull Request
Before you submit your pull request consider the following guidelines:

* Search GitLab for an open or closed Pull Request
  that relates to your submission. You don't want to duplicate effort.
* Make your changes in a new git branch:

    ```shell
    git checkout -b my-fix-branch master
    ```

* Create your patch, **including appropriate test cases**.
* Follow our [Coding Rules](#rules).

* Commit your changes using a descriptive commit message

    ```shell
    git commit -a
    ```
  Note: the optional commit `-a` command line option will automatically "add" and "rm" edited files.

* Build your changes locally to ensure all the tests pass:

    ```shell
    grunt test
    ```

* Push your branch to GitLab:

    ```shell
    git push origin my-fix-branch
    ```

In GitLab, send a pull request to `fornax:master`.
If we suggest changes, then:

* Make the required updates.
* Re-run the Formax test suite to ensure tests are still passing.
* Commit your changes to your branch (e.g. `my-fix-branch`).
* Push the changes to your GitLab repository (this will update your Pull Request).

If the PR gets too outdated we may ask you to rebase and force push to update the PR:

```shell
git rebase master -i
git push origin my-fix-branch -f
```


That's it! Thank you for your contribution!

#### After your pull request is merged

After your pull request is merged, you can safely delete your branch and pull the changes
from the main (upstream) repository:

* Delete the remote branch on GitLab either through the GitLab web UI or your local shell as follows:

    ```shell
    git push origin --delete my-fix-branch
    ```

* Check out the master branch:

    ```shell
    git checkout master -f
    ```

* Delete the local branch:

    ```shell
    git branch -D my-fix-branch
    ```

* Update your master with the latest upstream version:

    ```shell
    git pull --ff upstream master
    ```


## <a name="rules"></a> Coding Rules

 * Spaces after function names.
 * Spaces after parameter names like so `param : typ`.
 * Use `lowerCaseCamelCase`, also on properties.
 * Interfaces *do not* start with `I`. Can you use a function instead, perhaps?
 * Prefer flat namespaces.
 * Prefer namespaces containing *only* what a consumer needs to use – no
   utilities should be public if it can be helped.
 * Follow the existing style of the library.
 * For single-argument function calls, prefer `fn par` over `par |> fn`. For
   multiple-argument function calls, `par3 |> fn par1 par2` is OK.
 * No final newline in files, please.
 * Open specify `open` directives for the used namespaces and modules.
 * For variables: prefer `test` over `t`. Prefer `test` over
    `sequencedTestCode`.

## <a name="build"></a> Build process

 * Dependencies are controlled with [Paket](https://fsprojects.GitLab.io/Paket/)
 * Build process is using [FAKE](http://fsharp.GitLab.io/FAKE/)
 * To build application and run tests run `build.cmd` (on Windows) or `build.sh` (Linux / OSX)

