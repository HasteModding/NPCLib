using HarmonyLib;
using Landfall.Haste;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using System.Reflection;
using UnityEngine;
using Zorro.Core;

namespace NPCLib;

/*
 * -----------------------------------------
 * The software is provided "AS IS" under the MIT license.
 * Library is in Beta of some sorts.
 * Contribution is welcome.
 * -----------------------------------------
 * A simple dialog creater.
 * Can and will error.
 * Expect bugs. A lot of them.
 * Does not support reactions.
 * Does not support custom tags.
 * -----------------------------------------
*/

public enum Characters
{
	Captain,
	Courier,
	Keeper,
	Leader,
	Sage,
	Heir,
	Researcher,
	Dalil,
	Wraith,
	Grunt,
	Grunt2,
	Grunt3,
	Weeboh,
	Weeboh2,
	Weeboh3,
}

public struct InterCharacter
{
	internal InterCharacter(Characters character, InteractionCharacter interactonCharacter)
	{
		Character = character;
		switch (character)
		{
			case Characters.Captain:
				Name = "The Captain";
				officalName = "Captain";
				break;

			case Characters.Courier:
				Name = "Zoe";
				officalName = "Courier";
				break;

			case Characters.Keeper:
				Name = "Riza";
				officalName = "Keeper";
				break;

			case Characters.Leader:
				Name = "Ava";
				officalName = "Leader";
				break;

			case Characters.Sage:
				Name = "Daro";
				officalName = "Sage";
				break;

			case Characters.Heir:
				Name = "Niada";
				officalName = "Heir";
				break;

			case Characters.Researcher:
				Name = "Gan";
				officalName = "Researcher";
				break;

			case Characters.Wraith:
				Name = "Wraith";
				officalName = "Wraith";
				break;

			case Characters.Dalil:
				Name = "Dalil";
				officalName = "Dalil";
				break;

			case Characters.Grunt:
				Name = "Grunt";
				officalName = "Grunt";
				break;

			case Characters.Grunt2:
				Name = "Grunt";
				officalName = "Grunt2";
				break;

			case Characters.Grunt3:
				Name = "Grunt";
				officalName = "Grunt3";
				break;

			case Characters.Weeboh:
				Name = "Weeboh";
				officalName = "Weeboh";
				break;

			case Characters.Weeboh2:
				Name = "Weeboh";
				officalName = "Weeboh2";
				break;

			case Characters.Weeboh3:
				Name = "Weeboh";
				officalName = "Weeboh3";
				break;

			default:
				Name = "Undefined";
				officalName = "Default";
				break;
		}
		(InteractionCharacter = interactonCharacter).CharacterSprite = GetCharacterSprite();
	}

	internal Characters Character { get; set; } = default;
	internal InteractionCharacter InteractionCharacter { get; set; } = null!;
	internal string Name { get; set; } = string.Empty;
	private string officalName { get; set; } = string.Empty;

	private Sprite GetCharacterSprite()
	{
		return GameObject.Find($"GAME/Handlers/InteractionHandler/InteractionUI/Canvas/Characters/{officalName}/Image")
			.GetComponent<UnityEngine.UI.Image>().sprite;
	}
}

public class DialogBuilder : List<DialogEntry>, IDisposable
{
	private readonly NPC _npc;

	public DialogBuilder(NPC npc) => _npc = npc;

	public void Add(Characters c, string l) => Add(new DialogEntry(c, l));

	public void Dispose() => _npc.CommitDialog(this);
}

public class NPC
{
	internal static List<NPC> Instances = new();
	internal Interaction Interaction = new();
	internal List<InteractionLine> Lines = new();
	private List<InterCharacter> _characters = new();
	private InteractableCharacter _interactioncharacter = null!;

	public NPC(Transform character, Transform markerPoint, string interactionName, InteractionCameraRig rig = null!)
	{
		// Few of checks
		if (character == null) throw new ArgumentNullException(nameof(character), "Character is null.");
		if (markerPoint == null) throw new ArgumentNullException(nameof(markerPoint), "Marker point is null.");
		if (string.IsNullOrEmpty(interactionName)) throw new ArgumentException("Interaction name is null or empty.", nameof(interactionName));
		if (Instances.Any(i => i.Interaction.name == interactionName)) throw new ArgumentException($"Interaction name '{interactionName}' already exists.", nameof(interactionName));
		if (rig == null) rig = GameObject.Find("Hub_Characters/CaptainCameraRig").GetComponent<InteractionCameraRig>();

		// Setup the new interaction
		Interaction.name = interactionName;
		Interaction.factsToSet = [];
		Interaction.canBePlayedSeveralTimes = true;

		// Create a new InteractableCharacter component and add it to the Character gameobject.
		// Additionally we turn it off straight away so the Start() method does not execute.
		(_interactioncharacter = character.gameObject.AddComponent<InteractableCharacter>()).enabled = false;
		_interactioncharacter.questionMarkTarget = character;
		_interactioncharacter.interactionCenter = character;
		_interactioncharacter.unlockInteraction = Interaction;
		_interactioncharacter.cameraRig = rig;

		Instances.Add(this);
	}

	public NPC(GameObject character, Transform markerPoint, string interactionName, InteractionCameraRig rig = null!) :
		this(character.transform, markerPoint, interactionName, rig)
	{ }

	public NPC(Transform character, GameObject markerPoint, string interactionName, InteractionCameraRig rig = null!) :
		this(character, markerPoint.transform, interactionName, rig)
	{ }

	public NPC(GameObject character, GameObject markerPoint, string interactionName, InteractionCameraRig rig = null!) :
		this(character.transform, markerPoint.transform, interactionName, rig)
	{ }

	internal GameObject GameObject => _interactioncharacter.gameObject ?? null!;

	internal void CommitDialog(List<DialogEntry> dialogs)
	{
		// Null check
		if (dialogs == null) throw new ArgumentException("No dialog was provided.");
		UnityEngine.Debug.LogError("");

		// Loop through each dialog entry
		foreach (DialogEntry dialog in dialogs)
		{
			// Check if the line is null or empty
			if (string.IsNullOrEmpty(dialog.line))
			{
				Debug.LogError($"Line: '{dialog.line}' is null or empty. Skipping line.");
				continue;
			}

			// If the dialog character is not valid somehow, skip the line
			if (!Enum.IsDefined(typeof(Characters), dialog.character))
			{
				try { Debug.LogError($"Character: '{dialog.character.ToString()}' is invalid. Skipping line."); }
				catch { Debug.LogError($"Character you've put in your dialog is invalid. Skipping line."); }
				continue;
			}

			// Get character by name
			InterCharacter character = _characters.FirstOrDefault(c => c.Character == dialog.character);

			// If the character does not exist, we create a new one
			if (character.InteractionCharacter == null || string.IsNullOrEmpty(character.Name))
			{
				InteractionCharacter intactChar = new();
				_characters.Add(new(dialog.character, intactChar));
				character = _characters.Last();

				intactChar.DisplayName = new UnlocalizedString(character.Name);
				intactChar.CharacterColor = new Color(0.3216f, 0.1137f, 0.2923f, 1);
				intactChar.VocalBank = GameObject.Find("Hub_Characters/Captain/").GetComponent<InteractableCharacter>().character.VocalBank;
				intactChar.TalkingSprite = GameObject.Find("Hub_Characters/Captain/").GetComponent<InteractableCharacter>().character.TalkingSprite;

				intactChar.Ability = AbilityKind.BoardBoost;
			}

			InteractionLine interactionLine = new();
			interactionLine.line = new UnlocalizedString(dialog.line);
			interactionLine.character = character.InteractionCharacter;
			interactionLine.requirements = [];
			Lines.Add(interactionLine);
			Interaction.Lines = Lines.ToArray();
		}

		_interactioncharacter.character = Lines.First().character;
		_interactioncharacter.enabled = true;
	}

	internal bool ContainsLine(string line) => Lines.Any(_lines => _lines.line.GetLocalizedString() == line);
}

[Landfall.Modding.LandfallPlugin]
public class NPCLib
{
	// Do IL patching right when the mod registers.
	// This should not affect any other mods since the class only starts when player is in the Hub.
	static NPCLib()
	{
		UnityEngine.Debug.Log("[NPCLib]: Initializing IL Patching");
		ILPatching.InteractionCoroutine_Patch();
		ILPatching.InteractableCharacter_Patch();
		UnityEngine.Debug.Log("[NPCLib]: Finished IL Patching");
		UnityEngine.Debug.LogWarning("[NPCLib]: Good luck!");
	}
}

public record DialogEntry(Characters character, string line);

public static class ILPatching
{
	public static void InteractableCharacter_Patch()
	{
		// Get references to the methods and fields we need
		MethodInfo startMethod = typeof(InteractableCharacter).GetMethod("Start", BindingFlags.NonPublic | BindingFlags.Instance);
		if (startMethod == null) throw new Exception("Couldn't find Start method");

		MethodInfo awakeMethod = typeof(InteractableCharacter).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance);
		if (awakeMethod == null) throw new Exception("Couldn't find Awake method");

		// Tell the original lines to fuck off and insert our own method call
		new ILHook(awakeMethod, il =>
		{
			// Ee errrr
			ILCursor c = new ILCursor(il);

			// Move to first instruction
			c.Goto(0);

			// Remove all instructions
			while (c.Next != null) c.Remove();

			// Load 'this' (the InteractableCharacter instance)
			c.Emit(OpCodes.Ldarg_0);

			// False
			c.Emit(OpCodes.Ldc_I4_0);

			// Call method with "this" and false as arguments
			c.Emit(OpCodes.Call, typeof(ILPatching).GetMethod("ReImp_Awake", BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance));

			// Return, end of method
			c.Emit(OpCodes.Ret);
		});

		// Insert our ReImp at the start, avoids rewriting the whole start method since we only need a check, then continue.
		new ILHook(startMethod, il =>
		{
			// Ee errrr
			ILCursor c = new ILCursor(il);

			// Move to first instruction
			c.Goto(0);

			// Load 'this' (the InteractableCharacter instance)
			c.Emit(OpCodes.Ldarg_0);

			// Call method with "this" as argument
			c.Emit(OpCodes.Call, typeof(ILPatching).GetMethod("ReImp_Start", BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance));
		});
	}

	public static void InteractionCoroutine_Patch()
	{
		// Get methodbase using HarmonyX & MonoMod.Utils. AccessTools.Method uses every BindingFlag, so no need to set our own.
		MethodInfo methodInfo = AccessTools.Method(typeof(InteractionPlayer), "InteractionCoroutine");
		if (methodInfo == null) throw new Exception($"Method 'InteractionCoroutine' not found in 'InteractionPlayer' dumbass");

		// Check if we made an oopsie
		MethodBase methodSMT = methodInfo.GetStateMachineTarget();
		if (methodSMT == null) throw new Exception($"Method 'MoveNext' not found in 'InteractionCoroutine' dumbass");

		// Create new hook
		new ILHook(methodSMT, (ILContext il) =>
		{
			// Create new cursor
			ILCursor c = new(il);

			// Find the callvirt to ParseText
			MethodInfo parseTextMethod = typeof(TextTagParser<InteractionTag>)
				.GetMethod("ParseText", [typeof(string), typeof(List<InteractionTag>).MakeByRefType()]);

			// If for some reason the method is not found, throw an error
			if (!c.TryGotoNext(i => i.MatchCallvirt(parseTextMethod)))
				throw new Exception("Couldn't find ParseText callvirt");

			// Could break in the future
			c.GotoPrev(i => i.MatchLdloc(out var _));

			// Get Instance from NPC, making sure to use binding flags since Instances is null at this time
			c.Emit(OpCodes.Ldsfld, typeof(NPC).GetField("Instances", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic));

			// Also could break in the future. This gets the local variable (V_5) from the method.
			c.Emit(OpCodes.Ldloc_S, (byte)5);

			// Call method to check if line exists in any custom NPC text, returning a bool
			c.EmitDelegate<Func<List<NPC>, string, bool>>((instances, str) => instances.Any(i => i.ContainsLine(str)));

			// new condition
			ILLabel afterThrow = c.DefineLabel();

			c.Emit(OpCodes.Brfalse_S, afterThrow); // False, skip the throw

			// Create's "Skipping" text
			c.Emit(OpCodes.Ldstr, "Skipping");

			// Create's new Exception with the previous string
			c.Emit(OpCodes.Newobj, typeof(Exception).GetConstructor([typeof(string)]));

			// Makes it throw
			c.Emit(OpCodes.Throw);

			// After condition
			c.MarkLabel(afterThrow);
		});
	}

	public static void ReImp_Awake(InteractableCharacter instance, bool allow = false)
	{
		// This is because the default npc's have their shit setup in the unity hierarchy
		if (!instance.questionMarkTarget && !allow) { return; }

		// Default code from InteractableCharacter
		instance.State = new InteractableCharacter.StateMachine(instance.questionMarkTarget, instance);
		instance.State.RegisterState(new InteractableCharacter.NoneState());
		instance.State.RegisterState(new InteractableCharacter.HasInteractionState());
		instance.State.RegisterState(new InteractableCharacter.HasAbilityUnlockState());
		instance.State.SwitchState<InteractableCharacter.NoneState>(false);
	}

	public static void ReImp_Start(InteractableCharacter instance)
	{
		if (NPC.Instances?.Any(i => i.GameObject?.name == instance.name) ?? false)
		{
			ReImp_Awake(instance, true);
		}
	}
}
