# Chillaxins

Chillaxins is a Unity Editor package that lets you use non-destructive tools on [ChilloutVR](https://documentation.abinteractive.net/cck/) avatars, [Basis](https://github.com/dooly123/Basis) avatars, and [Warudo](https://warudo.app/) character mods.

Using this, you can use products like [⭐ Starmesh](https://docs.hai-vr.dev/docs/products/starmesh), [⭐ FaceTra Shape Creator](https://docs.hai-vr.dev/docs/products/facetra-shape-creator), and [Prefabulous Universal](https://docs.hai-vr.dev/docs/products/prefabulous) across supported platforms.

As of April 2026, Chillaxins is also commonly used to help with the installation of NDMF and Modular Avatar on Basis projects.

---

## 🌐 Supported Platforms

- 🌆 **ChilloutVR** (CCK 3.x and CCK 4+ with `CCKBuildProcessor`)
- 📹 **Warudo** (via the [Denormalized Avatar Exporter](https://docs.hai-vr.dev/docs/products/denormalized-avatar-exporter))
- 🌐 **Basis** (avatar projects)

---

## 📋 Requirements

- **Unity**: Unity 2019.4, 2021.3, or 2022.3 (matching your target platform SDK)
- **Framework**: [Non-Destructive Modular Framework (NDMF)](https://github.com/bdunderscore/ndmf) (supports NDMF 1.5.0 through 1.14.8+)
- **Package Dependencies**:
  - `com.unity.burst`: `1.6.6` (required for Modular Avatar / Unity 2021+)
  - `com.unity.collections`: `2.1.4` (required for modern NDMF)
  - `com.unity.mathematics`: `1.2.6` (required for modern NDMF)

---

## 📝 Notes & Limitations

- The non-destructive tools must be based on the [Non-Destructive Modular Framework (NDMF)](https://github.com/bdunderscore/ndmf) and must not depend on other incompatible SDKs, such as the VRChat SDK.
- To some extent, it might let you use platform-agnostic components of other tools such as [Modular Avatar Merge Armature](https://modular-avatar.nadena.dev/docs/reference/merge-armature) and [Modular Avatar Bone Proxy](https://modular-avatar.nadena.dev/docs/reference/bone-proxy).
- Chillaxins is compatible with Warudo projects if you use the [Denormalized Avatar Exporter](https://docs.hai-vr.dev/docs/products/denormalized-avatar-exporter).

---

## 📦 What Does Chillaxins Do?

### 1. Missing Library Wrapper
Prevents compilation errors in non-VRChat projects by providing runtime and editor dependencies that NDMF and Modular Avatar expect:
- **`0Harmony.dll`** ([Lib.Harmony](https://www.nuget.org/packages/Lib.Harmony/)): Required for NDMF patching and hooks.
- **`System.Collections.Immutable.dll`** ([Microsoft NuGet](https://www.nuget.org/packages/System.Collections.Immutable/)): Used extensively by NDMF solver and AST.
- **`System.Memory.dll`** & **`System.Buffers.dll`**: Provides `ReadOnlySpan<T>` and memory pooling for modern NDMF (1.10+ / 1.14.8+).
- **`System.Numerics.Vectors.dll`** & **`System.Runtime.CompilerServices.Unsafe.dll`**: Underpinning dependencies for high-performance data processing.

### 2. Avatar Build Pipeline Hooks (ChilloutVR)
- **Automatic Build Execution**: Hooks into the ChilloutVR avatar build and upload process (both legacy CCK and CCK 4+ `CCKBuildProcessor`). When you upload an avatar using the CCK interface, it automatically executes the NDMF non-destructive avatar processors.
- **Native Platform Provider (`ChilloutVRPlatform`)**: Registers `CVRAvatar` as a valid NDMF avatar root (`INDMFPlatformProvider`), ensuring NDMF's **Manual bake avatar** and automated pipelines detect and process ChilloutVR avatars seamlessly.

---

## 🛠️ Editor Menu & Options (`Tools > Chillaxins`)

Chillaxins adds convenient toggles under the Unity Editor menu:

- **`Disable Pre-Avatar Bundle Event`**:
  Disables automatic NDMF execution during CCK bundling. Useful if using plugins/components that do not work with automatic bundling (such as Magica Cloth 1) or when using a manual bake workflow.
- **`Disable Play Mode Upload Trigger`**:
  Prevents NDMF from executing when entering Play Mode as a result of an upload trigger.
- **`Safeguard Manual Bake Asset Editing`** *(Optional — Disabled by default)*:
  Protects against Unity Editor memory allocation crashes during NDMF Manual Bake. In NDMF 1.10+, manual bake wraps processing in `AssetDatabase.StartAssetEditing()`. When CCK generates AAS controllers with subassets, frozen asset imports can cause Unity to corrupt memory allocations. Enabling this safeguard dynamically pauses asset editing during controller generation and safely restores it afterward.

---

## 🚀 Installation

### Option 1: Unitypackage (Recommended)
1. Download the latest `.unitypackage` file from [GitHub Releases](https://github.com/hai-vr/chillaxins/releases).
2. Import the package into your Unity project (`Assets > Import Package > Custom Package...`).
3. **Close your Unity project completely.**
4. **Reopen your Unity project.**

> [!WARNING]
> Installing as UPM (Git URL) or as a tarball is not supported by some tools and may result in errors. Only install using the methods described here.

### Option 2: ALCOM Package Manager
If you use the [ALCOM](https://docs.hai-vr.dev/docs/products/listing) package manager, you can add the **Haï ~ Chillaxins** package directly from [our listing](https://docs.hai-vr.dev/docs/products/listing).

---

## 🔒 About the DLL Files

This package contains precompiled DLL files because NDMF depends on libraries that are not included with Unity by default. For convenience of installation across different environments, these DLLs are packaged with Chillaxins.

If you are concerned about safety, you can verify or supply the DLLs on your own from Microsoft NuGet:
- [System.Collections.Immutable](https://www.nuget.org/packages/System.Collections.Immutable/)
- [Lib.Harmony](https://www.nuget.org/packages/Lib.Harmony/)
- [System.Memory](https://www.nuget.org/packages/System.Memory/)
- [System.Buffers](https://www.nuget.org/packages/System.Buffers/)
- [System.Runtime.CompilerServices.Unsafe](https://www.nuget.org/packages/System.Runtime.CompilerServices.Unsafe/)
