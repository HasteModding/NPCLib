using Landfall.Haste;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System.Reflection;
using UnityEngine;
using Zorro.Core;
using static Landfall.Haste.ReactionUI;

namespace NPCLib;

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
			default:
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
		}
		InteractionCharacter = interactonCharacter;
		InteractionCharacter.CharacterSprite = GetCharacterSprite();
		InteractionCharacter.VocalBank = GetCharacterVocals(Name);
	}

	public Characters Character { get; set; } = default;
	public InteractionCharacter InteractionCharacter { get; set; } = null!;
	public string Name { get; set; } = string.Empty;
	public InteractionVocalBank VocalBank { get; set; } = null!;
	private string officalName { get; set; } = string.Empty;

	private Sprite GetCharacterSprite()
	{
		return GameObject.Find($"GAME/Handlers/InteractionHandler/InteractionUI/Canvas/Characters/{officalName}/Image")
			.GetComponent<UnityEngine.UI.Image>().sprite;
	}

	private InteractionVocalBank GetCharacterVocals(string name)
	{
		return Resources.FindObjectsOfTypeAll<InteractionVocalBank>()
			.FirstOrDefault(v => v.name.Contains(name, StringComparison.CurrentCultureIgnoreCase)) ?? null!;
	}
}

public class DialogBuilder : List<DialogEntry>, IDisposable
{
	private readonly NPC _npc;

	public DialogBuilder(NPC npc) => _npc = npc;

	public void Add(Characters c, string l, ReactionType e = ReactionType.Expressionless) => Add(new DialogEntry(c, l, e));

	public void Dispose() => _npc.CommitDialog(this);
}

public class NPC
{
	public NPC(Transform character, Transform markerPoint, string interactionName, InteractionCameraRig rig = null!, Action onComplete = null!)
	{
		// Few of checks
		if (character == null) throw new ArgumentNullException(nameof(character), "Character is null.");
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
		_interactioncharacter.questionMarkTarget = markerPoint;
		_interactioncharacter.interactionCenter = character;
		_interactioncharacter.unlockInteraction = Interaction;
		_interactioncharacter.cameraRig = rig;
		_interactioncharacter.onComplete = onComplete;

		Instances.Add(this);
	}

	public NPC(GameObject character, Transform markerPoint, string interactionName, InteractionCameraRig rig = null!, Action onComplete = null!) :
		this(character.transform, markerPoint, interactionName, rig, onComplete)
	{ }

	public NPC(Transform character, GameObject markerPoint, string interactionName, InteractionCameraRig rig = null!, Action onComplete = null!) :
		this(character, markerPoint.transform, interactionName, rig, onComplete)
	{ }

	public NPC(GameObject character, GameObject markerPoint, string interactionName, InteractionCameraRig rig = null!, Action onComplete = null!) :
		this(character.transform, markerPoint.transform, interactionName, rig, onComplete)
	{ }

	public NPC(GameObject character, Vector3 markerOffset, string interactionName, InteractionCameraRig rig = null!, Action onComplete = null!) :
		this(character.transform, (GameObject)null!, interactionName, rig, onComplete)
	{
		GameObject markerPoint = new GameObject("MarkerPoint");
		markerPoint.transform.SetParent(character.transform);
		markerPoint.transform.localPosition = markerOffset;
		_interactioncharacter.questionMarkTarget = markerPoint.transform;
	}

	public static List<NPC> Instances { get; set; } = new();
	public GameObject GameObject { get => _interactioncharacter.gameObject ?? null!; }
	public Interaction Interaction { get; set; } = new();
	public List<InteractionLine> Lines { get; set; } = new();
	public GameObject QuestionMarkObject { get => _interactioncharacter.questionMarkTarget.gameObject ?? null!; }
	private List<InterCharacter> _characters { get; set; } = new();
	private InteractableCharacter _interactioncharacter { get; set; } = null!;

	internal void CommitDialog(List<DialogEntry> dialogs)
	{
		// Null check
		if (dialogs == null) throw new ArgumentException("No dialog was provided.");
		UnityEngine.Debug.LogError("");

		Lines?.Clear();

		// Loop through each dialog entry
		foreach (DialogEntry dialog in dialogs)
		{
			// Check if the line is null or empty
			if (string.IsNullOrEmpty(dialog.line))
			{
				Debug.LogError($"Line: {dialog.line} is null or empty. Skipping line.");
				return;
			}

			// If the dialog character is not valid somehow, skip the line
			if (!Enum.IsDefined(typeof(Characters), dialog.character))
			{
				try { Debug.LogError($"Character: '{dialog.character.ToString()}' is invalid. Skipping line."); }
				catch { Debug.LogError($"Character you've put in your dialog is invalid. Skipping line."); }
				return;
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
				intactChar.TalkingSprite = GameObject.Find("Hub_Characters/Captain/").GetComponent<InteractableCharacter>().character.TalkingSprite;
				intactChar.Ability = AbilityKind.BoardBoost;
			}

			InteractionLine interactionLine = new();
			interactionLine.line = new UnlocalizedString(dialog.line);
			interactionLine.character = character.InteractionCharacter;
			interactionLine.requirements = [];
			Lines!.Add(interactionLine);
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
		ILPatching.InteractableCharacter_Patch();
		UnityEngine.Debug.Log("[NPCLib]: Finished IL Patching");
		UnityEngine.Debug.LogWarning("[NPCLib]: Good luck!");
	}
}

public record DialogEntry(Characters character, string line, ReactionType expression);

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

	public static void ReImp_Awake(InteractableCharacter instance)
	{
		// This is because the default npc's have their shit setup in the unity hierarchy
		// So if the instance does not have a questionMarkTarget we return
		if (!instance.questionMarkTarget) { return; }

		// Default code from InteractableCharacter
		instance.State = new InteractableCharacter.StateMachine(instance.questionMarkTarget, instance);
		instance.State.RegisterState(new InteractableCharacter.NoneState());
		instance.State.RegisterState(new InteractableCharacter.HasInteractionState());
		instance.State.RegisterState(new InteractableCharacter.HasAbilityUnlockState());
		instance.State.SwitchState<InteractableCharacter.NoneState>(false);
	}

	public static void ReImp_Start(InteractableCharacter instance)
	{
		if (instance.State == null)
		{
			ReImp_Awake(instance);
		}
	}
}
