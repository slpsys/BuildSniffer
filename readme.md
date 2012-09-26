BuildSniffer
========

Helpful in debugging complex (or legacy/terrible!) MSBuild ordering issues. It uses the Microsoft.Build.* APIs to evaluate build targets in the exact same order you'd see from just running `> msbuild my_complex_source_tree.proj`.

Usage
------

There is some manual labor involved. Typical usage (the way I use it) involves nerfing/sandboxing the build, so that all it can really do is compile or group targets. These are specified by passing a list of action tag names to an `IgnoreItems` method. This step seeks to make sure it's repeatable, that we don't accidentally overwrite something you needed (and didn't intend a build debugger to modfiy), and also to speed up the process. On our ginormous build proj file, it takes well under a second, because it's not moving around files, etc. Obviously, to do this, you must be somewhat familiar with your build. 

How it works:
--------------

BuildSniffer works in the following manner under the hood:
 
 * Read the MSBuild project XML
 * Strip it of any [potentially non-repeatable] actions that actually modify on-disk state of the build (e.g. `<Move>`). Also, strip out `<Message>` actions
 * Swap any `<MSBuild>` actions with similar `<Message>` actions, retaining the build target
 * Insert a special ILogger in the logging chain that listens for these Messages and records them
 * Have the MSBuild API actually try to build the nerfed, modified project XML document
 * Play back the log's record via an `IEnumerable<TargetResult>`