# NPCLib

### 🧠 What it does

- Adds dialogue support to your custom NPCs
- Handles sprite setup, camera rigs, and vocal banks
- IL-patches Haste’s `InteractableCharacter` to make custom NPCs just work

> [!CAUTION]
> **This library isn't fully tested**  
> Things may break. You’ve been warned.  
> Ping `ignoredsoul` on Discord if it all goes sideways.

---
<!--------------------------------------------------------------------------------------->

### ⚙️ Features

- **💬 Dialog System**  
  Use `DialogBuilder` to set up conversations effortlessly.

- **🧍 Custom Characters**  
  Define your NPCs using the `Characters` enum. NPCLib handles vocal banks and sprites.

- **🎥 Interaction Camera**  
  Use `InteractionCameraRig` to create cinematic closeups or cursed fisheye shots.

- **🛠️ IL Patching**  
  Seamlessly integrates with `InteractableCharacter`. No boilerplate needed.

---
<!--------------------------------------------------------------------------------------->

### 📦 Installation

1. Clone the repo into your projects directory (e.g., `./Projects/NPCLib`)
2. Edit your `.csproj` and add:

```xml
<ItemGroup>
  <Compile Include="..\NPCLib\*.cs">
    <Link>NPCLib\%(Filename)%(Extension)</Link>
  </Compile>
</ItemGroup>
```

> [!CAUTION]
> To add more context, we cloned the repo into `./Projects`.  
> Then in your project `./Projects/MyMod/` you edit the `.csproj` and add the xml.  

> [!NOTE]
> You can also copy the files directly into your own project if you hate XML.

---
<!--------------------------------------------------------------------------------------->

### **How to use NPCLib in your project**
<details> <summary>👤 Step 1: Create an NPC</summary>

**Option A – Use a Marker Point**
You can either create an `MarkerPoint` `GameObject` on your model for the marker to position itself
```cs
GameObject character = GameObject.Instantiate(MyCustomNPC);
Transform markerPoint = character.transform.Find("MarkerPoint");
NPC npc = new NPC(character, markerPoint, "MyUniqueInteractionName");
```
**Option B – Use an offset from the center**
```cs
GameObject character = GameObject.Instantiate(MyCustomNPC);
NPC npc = new NPC(character, new Vector3(0, 5, 0), "MyUniqueInteractionName");
```

</details> <details> <summary>💬 Step 2: Add Dialogue</summary>

*Multiple syntax options are supported. Choose what you like:*

**Builder with object initializer**
```cs
using (new DialogBuilder(npc)
{
  { Characters.Captain, "Hello." },
  { Characters.Courier, "Hi!" },
}) { }
```

**Builder with chained Add**
```cs
using (DialogBuilder builder = new DialogBuilder(npc))
{
  builder.Add(Characters.Captain, "Hello.");
  builder.Add(Characters.Courier, "Hi!");
}
```

**Pre-built and committed**
```cs
DialogBuilder dialog = new DialogBuilder(npc);
dialog.Add(Characters.Captain, "Hello.");
dialog.Add(Characters.Courier, "Hi!");
npc.CommitDialog(dialog);
```

**From list**
```cs
List<DialogEntry> dialog = new()
{
  new(Characters.Captain, "Hello."),
  new(Characters.Courier, "Hi!")
};
npc.CommitDialog(dialog);
```

</details> <details> <summary>🛠️ Step 3: Customize Behavior (Optional)</summary>

```cs
ExtraConfig config = new ExtraConfig
{
  CameraRig = customCameraRig,
  OnCreated = npc => Debug.Log("NPC spawned!"),
  OnComplete = npc => Debug.Log("Dialog done."),
  PersistantOnComplete = true
};
NPC npc = new NPC(character, markerPoint, "InteractionID", config);
```
</details>

---
<!--------------------------------------------------------------------------------------->

### ❓ FAQ

#### > What’s this for?
For modders making custom NPCs in Haste. It saves you from reinventing the ***FUCKING CURSED*** dialog system.

#### > Does it play nice with other mods?
Should work fine. May conflict with similar libraries if ones ever get made lol.

#### > What if I break something?
Open an issue or scream in the [Haste Modding Community](https://discord.gg/hastebrokenworlds).

---
<!--------------------------------------------------------------------------------------->

### 🤝 Contributing

Bug fixes, ideas, improvements—anything helps. Fork it, fix it, PR it.  
No guarantee I'll merge it, but I’ll suffer less because of you.

---
<!--------------------------------------------------------------------------------------->

### 📜 License

MIT. Do what you want. Just don’t blame me when it explodes.  
Credit’s nice. Not required. 🙏😭

---
<!--------------------------------------------------------------------------------------->

### Shoutouts

Big thanks to:
- **[Steve](https://github.com/Stevelion)** for bug testing, giving me pointers and keeping me sane. ily pookie.
- **[Hamunii](https://github.com/hamunii)** for teaching me more about VisualStudio xml shit.

---
<!--------------------------------------------------------------------------------------->

### **Final Words**
```
  ___   _   _    _  _____ _____   _______     _______ ______   _______ _   _ ___ _   _  ____ 
 |_ _| | | | |  / \|_   _| ____| | ____\ \   / / ____|  _ \ \ / /_   _| | | |_ _| \ | |/ ___|
  | |  | |_| | / _ \ | | |  _|   |  _|  \ \ / /|  _| | |_) \ V /  | | | |_| || ||  \| | |  _ 
  | |  |  _  |/ ___ \| | | |___  | |___  \ V / | |___|  _ < | |   | | |  _  || || |\  | |_| |
 |___| |_| |_/_/   \_\_| |_____| |_____|  \_/  |_____|_| \_\|_|   |_| |_| |_|___|_| \_|\____|
```

---
<!--------------------------------------------------------------------------------------->

</br>
<h3>Anyway, here's the update journey...</h3></br>
</br>

### Update 1.2.0
- Redid a some of the README to fit my style ig idk anymore I'm slow.
- General clean up.

### Update 1.1.0
- Added another contructor.
- Added persistant OnComplete to the ExtraConfig. Originally the action would clear upon calling it.
- Fixed failing to get the correct vocal bank.

### Update 1.0.0
- Official release! 🥳🥳🥳
- Updated README to be more informative.
- Still need to rework the ILPatching to check and handle multiple people using the library. Will fix when I can be bothered :3

### Update 0.2.1
- Added more XML documentation.
- Minor cleaning.

### Update 0.2
- Added an extra config struct.
- Added summaries 
- Reworked the InterCharacter class.
- Changed most methods and fields to be public.

### Update 0.1.2
- I hate patches.

### Update 0.1.1
- Fixed Weeboh and Grunt's voicelines not being found cause I am stupid. <sup><sub>Thank you Stevelion for the catch</sub></sup>

### Update 0.1
- Added expressions enum.
- Added new constructor for marker point offset instead of a transform.
- Added actions for when the dialog is finished.
- Added field to get the question mark gameobject.
- Changed the dialog commit, now clearing the previous lines before adding the new dialog.
- Changed and fixed instances field to get set method.
- Changed other stuff, i forgor 💀
- Removed TextTagParser patch, allowing expressions.

<!--

This will remain in "beta" for the time being. </br>
Strictly for teasing and contributing till we have a fully working system that meets a few unlisted criteria. </br>

(HOW THE FUCK DID I MANAGE TO WRITE "teasing" INSTEAD OF "testing" WHAT)

Mainly, importing custom characters. </br>
It's not necessarily hard to do, it's just trying to find a way to make it easier for the end-user. </br>

But feel free to mess around with it. Break it. Re-write it. Spit on it. Don't care. Go for gold. </br>
And if you want to use this, give credit where it's due and prepare for any bugs that will occur.
-->