# csmake

Born out of being frustrated with the minimal seperation of intent from implementation in the default VS provided C# build system, being too lazy to learn how to use CMake for C#, and a spare evening to spend trying it out.

Lightweight cmake-a-like meta-build system for C#.

Allows some amount of configuration via .csmake files in the filesystem, parsed recursively up the file system much like the various Git config files.

Can currently build itself as a dotnetcore 3.1 app. Don't use this for you own projects.