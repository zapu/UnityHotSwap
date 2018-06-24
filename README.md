# Unity Hot Patch

Swap code in Unity in play mode, without assembly reloading.

* [See a video example (45 sec, 25MB mp4).](https://zapu.keybase.pub/Unity/Hotpatching.mp4)

(Visual Studio Code is not required)

## What it does

UnityHotSwap recompiles project's C# assemblies and patches functions while the game is running in the editor. All objects are left untouched. Only methods that have changed are patched.

Only method bodies can be replaced. Types, fields, method cannot be added or removed (this creates some limitations with lambda expressions and Linq). Method signatures cannot change, even if all invocations of such methods are changed.

It probably does what Visual Studio C++ *Edit and continue* is able to do. Maybe it will do more in the future. Or maybe Unity drops mono altogether and this will become obsolete. Or maybe Unity rolls its own implementation of similar system and we will never have to exit play mode ever again.

UnityHotSwap is currently in pre-alpha stage and will probably not work for non-toy projects. Small assemblies and simple replacements might work, though, so you are free to play around. But if it breaks, you get to keep all the shiny pieces. Contributions are welcome.

## How to install and use

Extract released package to anywhere in your Asset folder. You should end up with 3 dll files:

* `ildynarec.dll`
* `unityhotswap.dll`
* `Mono.Cecil.dll`

If you are serious about using it, you should probably make them "Editor" only, or move to "Editor/" magic folder.

Now, while in *Play mode*, you should be able to make changes in your code and apply it by using *Experimental > Hot-patch project* (or `ALT+F10`).

## How does it work

UnityHotSwap does not need assembly instrumentation. If you are not hotpatching in current session, there is zero overhead, and your game plays as is.

When *Hot-patch* option is first used, all currently loaded assemblies are inspected and a dictionary of method full names and method body hashes is created. This is used to quickly compare method bodies to bodies in recompiled assemblies.

Then, every assembly is recompiled used mono command line arguments that Unity keeps in `Temp/` folder. UnityHotSwap will look for these settings and invoke mono to compile project assemblies to temporary output dll files. Then these new assemblies are loaded and UnityHotSwap will find methods which bodies have changed compared to currently running methods. See `UnityPlayModePatching.cs`.

DynamicMethod is emitted using `ILGenerator` and method body from `Mono.Cecil.MethodDefinition`. Method body from new assembly is essentially "retargetted" to run within currently loaded runtime. See `Recompiler.cs`

To swap currently loaded method with newly generated DynamicMethod, both methods are forced to be jit-compiled using `RuntimeHelpers.PrepareMethod`. Then a `jump` or `push+ret` code "gadget" is inserted into memory at the address of the original method, making it immediately continue execution at the new method's code after it's called. See `Hotpatcher.cs`

Some Unity trickery is needed for all this to work. When *Play mode* is activated, UnityHotSwap will try to disable assembly reloading using `EditorApplication.LockReloadAssemblies();`. Those settings are restored when *Play mode* is exited.

## Known issues

IL recompilation is an involved process and is definitely not feature complete. Mono is a lot more forgiving in terms of `DynamicMethod` generation so sometimes invalid code will crash the entire editor. Please do not report bugs to Unity when this happens, as they will be useless to them. Report them here instead. Thanks!

Some code will fail to recompile with various exceptions. You can try to reproduce with minimal function that fails to be compiled and file an issue. Thanks!

Behavior of virtual and override methods is not tested. Generic methods are not supported, this is a limitation of DynamicMethod generation. Calling generic method from new code is fine, though.

Sometimes Unity will recompile and reload assemblies after code changes even though we do our best to disable this behavior. When Unity starts ignoring `LockReloadAssemblies`, your best bet is to restart the editor. This tends to happen when *Pause* is used while in Play mode.

## How to contribute

Fork and clone this repository. I use Microsoft Visual Studio 2017 but whatever should be fine, this is not very huge codebase. Fix dependencies to `UnityEngine.dll` and `UnityEditor.dll`. Have Visual Studio download nuget packages (nunit).

If this project gets traction, I'll figure this out.

## License

GPLv2.

Since you should not be linking this library in your final product (but merely using it during development), license of your product does not matter and you are not required to release anything. But any released changes to the library itself must include source code.

## Contact

I can be reached on Keybase https://keybase.io/zapu or by e-mail: michal at zapu.net


***

## Historical purposes - how it used to work in 2015

*See above for current writeup, this is not how things work anymore.*

First, UnityHotSwap instruments assemblies and adds private static `DynamicMethod` field for each method that can be replaced at runtime (some limitations apply, e.g. constructors cannot be replaced at the moment). Then, each of those methods is instrumented to check if the associated dynamic method is not null. If it's not, it is invoked and its result is returned. Execution never reaches the original method body. See `Instrument.cs`.

When *Hot swap* menu item is used, UnityHotSwap will first build new version of assembly using last compiler parameters from Unity `Temp/` directory. Newly built assembly is then compared to the currently running one. Methods that have changed will be recompiled from static `Mono.Cecil.MethodDefinition` body to runtime `DynamicMethod` using `ILGenerator`. See `Recompiler.cs`.

Some Unity trickery is needed for all this to work. When *Play mode* is activated, UnityHotSwap will try to disable assembly reloading using `EditorApplication.LockReloadAssemblies();` and `EditorPrefs.SetBool("kAutoRefresh", false);`. Those settings are restored when *Play mode* is exited. There is an `InitializeOnLoad` class with static constructor that triggers the instrumentation. It is guaranteed to be invoked at some point after Unity discovers code modification and recompiles assembly.