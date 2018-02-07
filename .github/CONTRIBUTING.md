Contributing to audio-analysis
=========================

Help wanted
------------

We mark the most straightforward issues as "help wanted". This set of issues is the place to start if you are interested in contributing but new to the codebase.

- [QutEcoacoustics/audio-analysis - "up for grabs"](https://github.com/QutEcoacoustics/audio-analysis/labels/up%20for%20grabs)


Contribution "Bar"
------------------

Project maintainers will merge changes that improve the product significantly and broadly and that align with our roadmap.

Contributions must also satisfy the other published guidelines defined in this document.

DOs and DON'Ts
--------------

Please do:

* **DO** follow our (enforce by StyleCop)
* **DO** give priority to the current style of the project or file you're changing even if it diverges from the general guidelines.
* **DO** include tests when adding new features. When fixing bugs, start with
  adding a test that highlights how the current behavior is broken.
* **DO** keep the discussions focused. When a new or related topic comes up
  it's often better to create new issue than to side track the discussion.
* **DO** blog and tweet (or whatever) about your contributions, frequently!

Please do not:

* **DON'T** make PRs for style changes. 
* **DON'T** surprise us with big pull requests. Instead, file an issue and start
  a discussion so we can agree on a direction before you invest a large amount
  of time.
* **DON'T** commit code that you didn't write. If you find code that you think is a good fit, file an issue and start a discussion before proceeding.
* **DON'T** submit PRs that alter licensing related files or headers.

Commit Messages
---------------

Please format commit messages as follows (based on [A Note About Git Commit Messages](http://tbaggery.com/2008/04/19/a-note-about-git-commit-messages.html)):

```
Summarize change in 50 characters or less

Provide more detail after the first line. Leave one blank line below the
summary and wrap all lines at 72 characters or less.

If the change fixes an issue, leave another blank line after the final
paragraph and indicate which issue is fixed in the specific format
below.

Fix #42
```

Also do your best to factor commits appropriately, not too large with unrelated things in the same commit, and not too small with the same small change applied N times in N different commits.

File Headers
------------

StyleCop automatically suggest an appropriate file header. Please use it at the top of all new files.


Copying Files from Other Projects
---------------------------------

We sometimes use files from other projects, typically where a binary distribution does not exist or would be inconvenient.

The following rules must be followed for PRs that include files from another project:

- The license of the file is [permissive](https://en.wikipedia.org/wiki/Permissive_free_software_licence).
- The license of the file is left in-tact.

Porting Files from Other Projects
---------------------------------

There are many good algorithms implemented in other languages that would benefit the .NET Core project.
The rules for porting an R file to C#, for example, are the same as would be used for copying the same file, as described above.