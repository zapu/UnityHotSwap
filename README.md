# Unity hot swap

Swap code in Unity in play mode, without assembly reloading.

* [See shorter example on gfycat (10 sec).](http://gfycat.com/HandmadeFastAnole)
* [See longer example on imgrush (40 sec).](https://imgrush.com/TnDsx0wnsqWd/direct)

(Visual Studio is not required)

## What it does

UnityHotSwap recompiles project's C# code and replaces it while the game is running in the editor. All objects are left untouched, only modified methods are replaced.

Only method bodies can be replaced. Types, fields, method cannot be added or removed (this creates some limitations with lambda expressions and Linq). Method signatures cannot change, even if all invocations of such methods are changed.

It probably does what Visual Studio C++ *Edit and continue* is able to do. Maybe it will do more in the future. Or maybe Unity drops mono altogether and this will become obsolete.

UnityHotSwap is currently in pre-alpha stage and will probably not work for non-toy projects. Small assemblies and simple replacements might work, though, so you are free to play around. But if it breaks, you get to keep all the shiny pieces. Contributions are welcome.

## How to install and use

Extract release to your `Assets` folder.

Your DLLs should end up as follows:

* `Dynarec/Editor/ildynarec.dll`
* `Dynarec/Editor/Mono.Cecil.dll`
* `Dynarec/Editor/unityhotswap.dll`

Open *Tools > Hot-Swap Settings* and set *Hotpatching* to enabled.

Now, while in *Play mode*, you should be able to make changes in your code and apply it by using *Tools > Hot-Swap* (or `ALT+F10`).

## How does it work

First, UnityHotSwap instruments assemblies and adds private static `DynamicMethod` field for each method that can be replaced at runtime (some limitations apply, e.g. constructors cannot be replaced at the moment). Then, each of those methods is instrumented to check if the associated dynamic method is not null. If it's not, it is invoked and its result is returned. Execution never reaches the original method body. See `Instrument.cs`.

When *Hot swap* menu item is used, UnityHotSwap will first build new version of assembly using last compiler parameters from Unity `Temp/` directory. Newly built assembly is then compared to the currently running one. Methods that have changed will be recompiled from static `Mono.Cecil.MethodDefinition` body to runtime `DynamicMethod` using `ILGenerator`. See `Recompiler.cs`.

Some Unity trickery is needed for all this to work. When *Play mode* is activated, UnityHotSwap will try to disable assembly reloading using `EditorApplication.LockReloadAssemblies();` and `EditorPrefs.SetBool("kAutoRefresh", false);`. Those settings are restored when *Play mode* is exited. There is an `InitializeOnLoad` class with static constructor that triggers the instrumentation. It is guaranteed to be invoked at some point after Unity discovers code modification and recompiles assembly.

## Known issues

IL recompilation is an interesting process and is definitely not feature complete. Mono is a lot more forgiving in terms of `DynamicMethod` generation so sometimes invalid code will crash the entire editor. Please do not report bugs to Unity when this happens, as they will be useless to them. Report them here instead. Thanks!

Behavior of virtual and override methods is... not tested.

Sometimes Unity will recompile and reload assemblies after code changes even though we do our best to disable this behavior.

Sometimes assembly will not get instrumented. Errors "method is not hotpatchable" is a result of this.

## How to contribute

Fork and clone this repository. I use Microsoft Visual Studio 2013 but whatever should be fine, this is not very huge codebase. Fix dependencies to `UnityEngine.dll` and `UnityEditor.dll`. Have Visual Studio download nuget packages (nunit).

If this project gets any traction I'll figure this out.

## License

GPLv2.

Since you are not linking this library in your final product (but merely using it during development), license of your product does not matter and you are not required to release anything. But any released changes to the library itself must include source code.

## Contact

I can be reached on twitter [@taluhunusa](https://twitter.com/taluhunusa) or by e-mail: michal at zapu.net
