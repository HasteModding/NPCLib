### <b><u>NPCLib</u></b>

When it comes to adding interaction dialogues to your custom NPCs in Haste, NPCLib is here to help.
This library contains all the resources you need to make your NPCs come to life, whether you're making a witty sage, a quirky merchant, or just a grunt who says "ugh." *I hope*

---
<!--------------------------------------------------------------------------------------->

### <b><u>What does NPCLib do?</u></b>

NPCLib is a modding tool that helps you:
- Give your custom NPCs interaction dialogs.
- Handle NPC interactions, behaviors, character sprites, vocal banks, and dialogue sequences with ease.
- Patch Haste's `InteractableCharacter` class to make it work with your custom NPCs without the stupid fucking complications of unity.

---
<!--------------------------------------------------------------------------------------->

### <b><u>Features</u></b>

- **Dialog System**:  
  Use the `DialogBuilder` to create and oversee dialog sequences. A slight simplification of the original bullshit.

- **Custom Characters**:  
  Define your NPCs using the `Characters` enum, and then let NPCLib take care of the rest, including vocal banks and sprites.

- **Interaction System**:  
  With a few lines of code, you can give your NPCs unique camera setups using the `InteractionCameraRig` class.

- **IL Patching**:  
  NPCLib patches the `InteractableCharacter` class in Unity, allowing you to concentrate on your mod without worrying about breaking the fucking game.

---
<!--------------------------------------------------------------------------------------->

### <b1><u>Installation</u></b>

You can "install" this library by either just slapping it's code into your project or the way I intended to do it:

1. Clone this repository into your mod directory
2. Edit your .csproj file.
3. Slap in the following XML;
```xml
<ItemGroup>
	<Compile Include="..\NPCLib\*.cs">
		<Link>NPCLib\%(Filename)%(Extension)</Link>
	</Compile>
</ItemGroup>
```

---
<!--------------------------------------------------------------------------------------->

### <b><u>How to use NPCLib in your project</u></b>

#### Step 1: Create an NPC

You can either create an MarkerPoint GameObject on your model for the marker to position itself
```cs
GameObject character = GameObject.Instantiate(MyCustomNPC);
Transform markerPoint = character.transform.Find("MarkerPoint");
NPC npc = new NPC(character, markerPoint, "MyUniqueInteractionName");
```
Or create an offset from the models center position. In this case, 5 units above the model.
```cs
GameObject character = GameObject.Instantiate(MyCustomNPC);
NPC npc = new NPC(character, new Vector3(0, 5, 0), "MyUniqueInteractionName");
```

---
<!--------------------------------------------------------------------------------------->

#### Step 2: Add Dialog

There are multiple ways of creating dialog for your NPC.
```cs
NPC npc = ...; // Your NPC instance
using (new DialogBuilder(npc)
{
    { Characters.Captain, "Hello." },
    { Characters.Courier, "Hi!" },
}) { }
```
```cs
NPC npc = ...; // Your NPC instance
using (DialogBuilder builder = new DialogBuilder(npc))
{
	builder.Add(Characters.Captain, "Welcome to the ship!");
	builder.Add(Characters.Courier, "I have a message for you.");
}
```
```cs
NPC npc = ...; // Your NPC instance
DialogBuilder dialog = new DialogBuilder(npc);
dialog.Add(Characters.Captain, "Hello.");
dialog.Add(Characters.Courier, "Hi!");
npc.CommitDialog(dialog);
```
```cs
NPC npc = ...; // Your NPC instance
List<DialogEntry> dialog2 = new List<DialogEntry>()
{
    new(Characters.Captain, "Hello."),
    new(Characters.Courier, "Hi!")
};
npc.CommitDialog(dialog2);
```

Yeah, pick your poison.

---
<!--------------------------------------------------------------------------------------->

#### Step 3: Customize Behavior (Optional)

Want to tweak how your NPC behaves? Use the `ExtraConfig` struct to add custom camera rigs or actions:
```cs
ExtraConfig config = new ExtraConfig
{
	cameraRig = customCameraRig,
	onComplete = npc => Debug.Log("Dialog finished!"),
	onCreated = npc => Debug.Log("NPC created!")
};
NPC npc = new NPC(character, markerPoint, "CustomInteraction", config);
```

---
<!--------------------------------------------------------------------------------------->

### <b><u>FAQ</u></b>

### **What is this for?**
NPCLib is for modders of **Haste** who want to add interaction dialogs to their custom NPCs.</br>
If you're making a mod with NPCs, this is for you... Seriously just use this rather than messing around yourself 😭

### **Does it work with other mods?**
I fucking hope so.

### **What if I break something?**
No worries! Open an issue or tell me in the [Haste Modding Community](https://discord.gg/hastebrokenworlds) server.


### **Contributing**

Got ideas? Found a bug? Want to add something cool? Fork the repo, make your changes, and send a pull request. Contributions are always welcome! </br>
Seriosuly, this project is hell.

---
<!--------------------------------------------------------------------------------------->

### <b><u>License</u></b>

This project is licensed under the MIT License. Do whatever you want with it, just don’t blame me if shit breaks. </br>
But if possible, just add me to some sort of credits 😭🙏

---
<!--------------------------------------------------------------------------------------->

### <b><u>Shoutouts</u></b>

Big thanks to:
- **Steve** for bug testing, giving me pointers and keeping me sane. ily pookie.
- **[Hamunii](https://github.com/hamunii)** for teaching me more about VisualStudio xml shit.

---
<!--------------------------------------------------------------------------------------->

### <b><u>Final Words</u></b>
<div align="center" style="font-size:50px">

**I HATE EVERYTHING**
</div>

---
<!--------------------------------------------------------------------------------------->

</br>
<h3><u>Anyway, here's the update journey...</u></h3></br>
</br>

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