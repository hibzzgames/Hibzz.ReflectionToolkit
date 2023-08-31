# Hibzz.ReflectionToolkit
![LICENSE](https://img.shields.io/badge/LICENSE-CC--BY--4.0-ee5b32?style=for-the-badge) [![Twitter Follow](https://img.shields.io/badge/follow-%40hibzzgames-1DA1f2?logo=twitter&style=for-the-badge)](https://twitter.com/hibzzgames) [![Discord](https://img.shields.io/discord/695898694083412048?color=788bd9&label=DIscord&style=for-the-badge)](https://discord.gg/YXdJ8cZngB) ![Unity](https://img.shields.io/badge/unity-%23000000.svg?style=for-the-badge&logo=unity&logoColor=white) ![C#](https://img.shields.io/badge/c%23-%23239120.svg?style=for-the-badge&logo=c-sharp&logoColor=white)

***An internal toolset used to examine assemblies and types using reflection***

<br>

## Installation
**Via Github**
This package can be installed in the Unity Package Manager using the following git URL.
```
https://github.com/hibzzgames/Hibzz.ReflectionToolkit.git
```

Alternatively, you can download the latest release from the [releases page](https://github.com/hibzzgames/Hibzz.ReflectionToolkit/releases) and manually import the package into your project.

<br>

## Usage

![toolkit inspector](https://github.com/hibzzgames/Hibzz.ReflectionToolkit/assets/37605842/78983314-1a07-43eb-ae33-2d579685f425)

Launch the Reflection Inspector from the `Hibzz > Launch Reflection Inspector` menu. By default, all the assemblies are listed. Using simple click interactions, the user can inspect the types and members along with their different properties.

The command field can be used to also inspect. However, this system is subject to change during the preview phase. Currently supported list of commands:
- select
  - `-a <assembly name>` - select an assembly
  - `-t <type name>` - select a type
- list
  - `-a` - list all assemblies
  - `-t` - list all types in selected assembly
  - `-m` - list all members in selected type

<br>

## Have a question or want to contribute?
If you have any questions or want to contribute, feel free to join the [Discord server](https://discord.gg/YXdJ8cZngB) or [Twitter](https://twitter.com/hibzzgames). I'm always looking for feedback and ways to improve this tool. Thanks!

Additionally, you can support the development of these open-source projects via [GitHub Sponsors](https://github.com/sponsors/sliptrixx) and gain early access to the projects.

