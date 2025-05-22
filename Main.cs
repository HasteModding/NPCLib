using Landfall.Haste;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using pworld.Scripts.Extensions;
using System.Reflection;
using UnityEngine;
using Zorro.Core;

/// <summary>
/// All supported dialog characters for the NPC system.
/// </summary>
public enum Characters
{
	Captain,
	Courier,
	Dalil,
	Grunt,
	Grunt2,
	Grunt3,
	Heir,
	Keeper,
	Leader,
	Researcher,
	Sage,
	Weeboh,
	Weeboh2,
	Weeboh3,
	Wraith,
}

public struct ExtraConfig
{
	/// <summary>
	/// Set's a custom camera rig for the NPC.
	/// </summary>
	public InteractionCameraRig CameraRig;

	/// <summary>
	/// Invoke action when the NPC dialog is finished
	/// </summary>
	public Action<NPC> OnComplete;

	/// <summary>
	/// Invoke action when the NPC is created.
	/// </summary>
	public Action<NPC> OnCreated;

	/// <summary>
	/// Set's the NPC OnComplete action to be persistant after the dialog is finished.
	/// </summary>
	public bool PersistantOnComplete;

	/// <summary>
	/// Initializes a new instance of the <see cref="ExtraConfig"/> struct with default values.
	/// </summary>
	public ExtraConfig()
	{
		CameraRig = null!;
		OnComplete = null!;
		OnCreated = null!;
		PersistantOnComplete = false;
	}
}

/// <summary>
/// Represents a dialog character with associated data such as name, interaction character, and vocal bank.
/// Used for dialog interactions in the NPC system.
/// </summary>
public struct InterCharacter
{
	internal InterCharacter(Characters character, InteractionCharacter interactonCharacter)
	{
		Character = character;
		Name = character switch
		{
			Characters.Captain => "The Captain",
			Characters.Courier => "Zoe",
			Characters.Keeper => "Riza",
			Characters.Leader => "Ava",
			Characters.Sage => "Daro",
			Characters.Heir => "Niada",
			Characters.Researcher => "Gan",
			Characters.Wraith => "Wraith",
			Characters.Dalil => "Dalil",
			Characters.Grunt or Characters.Grunt2 or Characters.Grunt3 => "Grunt",
			Characters.Weeboh or Characters.Weeboh2 or Characters.Weeboh3 => "Weeboh",
			_ => "The Captain",
		};

		InteractionCharacter = interactonCharacter;
		InteractionCharacter.CharacterSprite = GetCharacterSprite();
		InteractionCharacter.VocalBank = GetCharacterVocals(Character.ToString());
	}

	/// <summary>
	/// Save the character enum that is used in the dialog.
	/// </summary>
	public Characters Character { get; set; } = default;

	/// <summary>
	/// Creates the <seealso cref="InteractionCharacter"/> that is used in the dialog.
	/// </summary>
	public InteractionCharacter InteractionCharacter { get; set; } = null!;

	/// <summary>
	/// The name of the character that is used in the dialog.
	/// </summary>
	public string Name { get; set; } = string.Empty;

	/// <summary>
	/// The <seealso cref="InteractionVocalBank"/> that the character uses.
	/// </summary>
	public InteractionVocalBank VocalBank { get; set; } = null!;

	/// <summary>
	/// Gets the sprite for the current character from the UI hierarchy.
	/// </summary>
	private Sprite GetCharacterSprite()
	{
		return GameObject.Find($"GAME/Handlers/InteractionHandler/InteractionUI/Canvas/Characters/{Character.ToString()}/Image")
			.GetComponent<UnityEngine.UI.Image>().sprite;
	}

	/// <summary>
	/// Gets the <see cref="InteractionVocalBank"/> for the specified character name.
	/// </summary>
	private InteractionVocalBank GetCharacterVocals(string characterName)
	{
		if (characterName == "Leader") characterName = "Ava";
		else if (characterName.ToLower().Contains("grunt")) characterName = "Wraith"; // Think it suits them best? idk.
		else if (characterName.ToLower().Contains("weeboh")) characterName = "Weeboh"; // Think it suits them best? idk.
		else characterName = Character.ToString();
		return Resources.FindObjectsOfTypeAll<InteractionVocalBank>()
			.FirstOrDefault(v => v.name.Contains(characterName, StringComparison.CurrentCultureIgnoreCase)) ?? null!;
	}
}

public static class ILPatching
{
	/// <summary>
	/// Overrides the Start and Awake methods of the <seealso cref="InteractableCharacter"/> class that is inherited from the <seealso cref="MonoBehaviour"/> class."
	/// </summary>
	/// <exception cref="Exception"></exception>
	public static void InteractableCharacter_Patch()
	{
		// Get references to the methods and fields we need
		MethodInfo startMethod = typeof(InteractableCharacter).GetMethod("Start", BindingFlags.NonPublic | BindingFlags.Instance);
		if (startMethod == null) throw new Exception("Couldn't find Start method");

		MethodInfo awakeMethod = typeof(InteractableCharacter).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance);
		if (awakeMethod == null) throw new Exception("Couldn't find Awake method");

		MethodInfo completeMethod = typeof(InteractableCharacter).GetMethod("OnComplete", BindingFlags.NonPublic | BindingFlags.Instance);
		if (completeMethod == null) throw new Exception("Couldn't find the OnComplete method");

		// Tell the original lines to fuck off and insert our own method call
		new ILHook(awakeMethod, il =>
		{
			// Ee errrr
			ILCursor c = new ILCursor(il);

			// Move to first instruction
			c.Goto(0);

			if (!c.TryFindNext(out _, instr => instr.MatchLdstr("PATCH_MARKER:NPCLIB")))
			{
				c.Emit(OpCodes.Ldstr, "PATCH_MARKER:NPCLIB");
				c.Emit(OpCodes.Pop);
			}
			else { UnityEngine.Debug.LogWarning("[NPCLib] InteractableCharacter awake method has already been patched. Skipping."); return; }

			// Remove all instructions
			while (c.Next != null) c.Remove();

			c.Emit(OpCodes.Ldstr, "PATCH_MARKER:NPCLIB");
			c.Emit(OpCodes.Pop); // discard the string

			// Load 'this' (the InteractableCharacter instance)
			c.Emit(OpCodes.Ldarg_0);

			// Call method with "this" and false as arguments
			c.Emit(OpCodes.Call, typeof(ILPatching).GetMethod("ReImp_Awake", BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance));

			// Return, end of method
			c.Emit(OpCodes.Ret);
			UnityEngine.Debug.LogWarning("[NPCLib] Patched Awake method");
		});

		// Insert our ReImp at the start, avoids rewriting the whole start method since we only need a check, then continue.
		new ILHook(startMethod, il =>
		{
			// Ee errrr
			ILCursor c = new ILCursor(il);

			// Move to first instruction
			c.Goto(0);

			// If the first instruction is the patch marker, we skip since it's already been patched
			if (!c.TryFindNext(out _, instr => instr.MatchLdstr("PATCH_MARKER:NPCLIB")))
			{
				c.Emit(OpCodes.Ldstr, "PATCH_MARKER:NPCLIB");
				c.Emit(OpCodes.Pop);
			}
			else { UnityEngine.Debug.LogWarning("[NPCLib] InteractableCharacter start method has already been patched. Skipping."); return; }

			c.Emit(OpCodes.Ldstr, "PATCH_MARKER:NPCLIB");
			c.Emit(OpCodes.Pop); // discard the string

			// Load 'this' (the InteractableCharacter instance)
			c.Emit(OpCodes.Ldarg_0);

			// Call method with "this" as argument
			c.Emit(OpCodes.Call, typeof(ILPatching).GetMethod("ReImp_Start", BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance));
			UnityEngine.Debug.LogWarning("[NPCLib] Patched Start method");
		});

		new ILHook(completeMethod, il =>
		{
			ILCursor c = new ILCursor(il);

			// Go to the last instruction
			c.Goto(il.Body.Instructions.Count - 1);

			// Move back if last instruction is ret, or just emit before it
			if (c.Next.OpCode != OpCodes.Ret)
				throw new Exception("Expected method to end in a 'ret' opcode");

			// Inject call to NPC.ShouldSkipOCN(this);
			c.Emit(OpCodes.Ldarg_0);
			c.Emit(OpCodes.Call, typeof(NPC).GetMethod("ShouldSkipOCN", BindingFlags.Public | BindingFlags.Static));

			UnityEngine.Debug.LogWarning($"[NPCLib ({Assembly.GetExecutingAssembly().GetName().Name})] Patched OnComplete method");
		});
	}

	/// <summary>
	/// Replacement implementation for the <c>Awake</c> method of <see cref="InteractableCharacter"/>.
	/// Sets up the state machine and registers default states if the instance is valid.
	/// </summary>
	/// <param name="instance">The <see cref="InteractableCharacter"/> instance to initialize.</param>
	public static void ReImp_Awake(InteractableCharacter instance)
	{
		// This is because the default npc's have their shit setup in the unity hierarchy
		// So if the instance does not have a questionMarkTarget, we return
		if (!instance?.questionMarkTarget) { return; }

		// Default code from InteractableCharacter
		instance!.State = new InteractableCharacter.StateMachine(instance.questionMarkTarget, instance);
		instance.State.RegisterState(new InteractableCharacter.NoneState());
		instance.State.RegisterState(new InteractableCharacter.HasInteractionState());
		instance.State.RegisterState(new InteractableCharacter.HasAbilityUnlockState());
		instance.State.SwitchState<InteractableCharacter.NoneState>(false);
	}

	/// <summary>
	/// Replacement implementation for the <c>Start</c> method of <see cref="InteractableCharacter"/>.
	/// Invokes <see cref="ReImp_Awake"/> if the state machine is not already initialized.
	/// </summary>
	/// <param name="instance">The <see cref="InteractableCharacter"/> instance to initialize.</param>
	public static void ReImp_Start(InteractableCharacter instance) => ReImp_Awake(instance.State == null ? instance : null!);
}

/// <summary>
/// A builder class for constructing dialog sequences associated with an NPC.
/// Automatically commits the dialog to the NPC when disposed.
/// </summary>
public class DialogBuilder : List<DialogEntry>, IDisposable
{
	private readonly NPC _npc;

	/// <summary>
	/// Initializes a new <see cref="DialogBuilder"/> for the specified NPC.
	/// </summary>
	/// <param name="npc">The NPC to associate with this dialog builder.</param>
	public DialogBuilder(NPC npc) => _npc = npc;

	/// <summary>
	/// Adds a new dialog entry for the specified character and line.
	/// </summary>
	/// <param name="c">The character speaking the line.</param>
	/// <param name="l">The dialog line text.</param>
	public void Add(Characters c, string l) => Add(new DialogEntry(c, l));

	/// <summary>
	/// Calls the <see cref="NPC.CommitDialog"/> method once the using is finished.
	/// Making it simplier for the user to create a dialog.
	/// </summary>
	public void Dispose() => _npc.CommitDialog(this);
}

/// <summary>
/// Represents a non-player character (NPC) with an associated <seealso cref="Interaction"/>, <seealso cref="DialogBuilder"/> system, and <seealso cref="InteractableCharacter"/> logic.<br/>
/// Supports multiple constructor overloads for flexibility in setup, and manages its own lifecycle within a static list of instances.
/// </summary>
public class NPC
{
	/// <summary>
	/// Initializes a new NPC instance by validating inputs, setting up the <seealso cref="Interaction"/>,
	/// attaching an <seealso cref="InteractableCharacter"/> component to the character, and storing optional configuration.
	/// </summary>
	/// <param name="character">The character transform to attach the interaction to.</param>
	/// <param name="markerPoint">The marker point for the interaction's question mark UI.</param>
	/// <param name="interactionName">A unique name for the interaction.</param>
	/// <param name="exc">Optional extra configuration for the interaction setup.</param>
	/// <exception cref="ArgumentNullException">Thrown if character is null.</exception>
	/// <exception cref="ArgumentException">Thrown if interactionName is null, empty, or already used.</exception>
	public NPC(Transform? character, Transform? markerPoint, string interactionName, ExtraConfig exc = default)
	{
		if (character == null)
		{ throw new ArgumentNullException(nameof(character), "Character is null."); }

		if (string.IsNullOrEmpty(interactionName))
		{ throw new ArgumentException("Interaction name is null or empty.", nameof(interactionName)); }

		if (exc.CameraRig == null)
		{ exc.CameraRig = GameObject.Find("Hub_Characters/CaptainCameraRig").GetComponent<InteractionCameraRig>(); }

		if (!hasInit)
		{
			UnityEngine.Debug.Log($"[NPCLib]: Registering {Assembly.GetExecutingAssembly().GetName().Name}");
			UnityEngine.Debug.Log("[NPCLib]: Initializing IL Patching");
			ILPatching.InteractableCharacter_Patch();
			UnityEngine.Debug.Log("[NPCLib]: Finished IL Patching");
			hasInit = true;
		}

		// Try to find an existing instance by interactionName.
		interaction = instances.Select(i => i.interaction).FirstOrDefault(i => i.name == interactionName);

		// If it doesn't exist, then we create a new one
		if (interaction == null)
		{
			// Setup the new interaction
			interaction = (Interaction)ScriptableObject.CreateInstance(nameof(Interaction));
			interaction.name = interactionName;
			interaction.factsToSet = [];
			interaction.canBePlayedSeveralTimes = true;
		}
		else { UnityEngine.Debug.LogWarning($"[NPCLib ({Assembly.GetExecutingAssembly().GetName().Name})]: Interaction name '{interactionName}' already exists. Using existing."); }

		// Create a new InteractableCharacter component and add it to the Character gameobject.
		// Additionally we turn it off straight away so the Start() method does not execute.
		(interactionCharacter = character.gameObject.GetOrAddComponent<InteractableCharacter>()).enabled = false;
		interactionCharacter.questionMarkTarget = markerPoint;
		interactionCharacter.interactionCenter = character;
		interactionCharacter.unlockInteraction = interaction;
		interactionCharacter.cameraRig = exc.CameraRig;
		interactionCharacter.onComplete = (exc.OnComplete != null ? () => { exc.OnComplete?.Invoke(this); } : null);

		_extraConfig = exc;
		characters = new();
		instances.Add(this);
	}

	/// <summary>
	/// Initializes a new NPC instance by validating inputs, setting up the <seealso cref="Interaction"/>,
	/// attaching an <seealso cref="InteractableCharacter"/> component to the character, and storing optional configuration.
	/// </summary>
	/// <param name="character">The character GameObject to attach the interaction to.</param>
	/// <param name="markerPoint">The marker point for the interaction's question mark UI.</param>
	/// <param name="interactionName">A unique name for the interaction.</param>
	/// <param name="exc">Optional extra configuration for the interaction setup.</param>
	/// <exception cref="ArgumentNullException">Thrown if character is null.</exception>
	/// <exception cref="ArgumentException">Thrown if interactionName is null, empty, or already used.</exception>
	public NPC(GameObject? character, Transform? markerPoint, string interactionName, ExtraConfig exc = default) :
		this(character?.transform, markerPoint, interactionName, exc)
	{ }

	/// <summary>
	/// Initializes a new NPC instance by validating inputs, setting up the <seealso cref="Interaction"/>,
	/// attaching an <seealso cref="InteractableCharacter"/> component to the character, and storing optional configuration.
	/// </summary>
	/// <param name="character">The character transform to attach the interaction to.</param>
	/// <param name="markerPoint">The marker point for the interaction's question mark UI.</param>
	/// <param name="interactionName">A unique name for the interaction.</param>
	/// <param name="exc">Optional extra configuration for the interaction setup.</param>
	/// <exception cref="ArgumentNullException">Thrown if character is null.</exception>
	/// <exception cref="ArgumentException">Thrown if interactionName is null, empty, or already used.</exception>
	public NPC(Transform? character, GameObject? markerPoint, string interactionName, ExtraConfig exc = default) :
		this(character, markerPoint?.transform!, interactionName, exc)
	{ }

	/// <summary>
	/// Initializes a new NPC instance by validating inputs, setting up the <seealso cref="Interaction"/>,
	/// attaching an <seealso cref="InteractableCharacter"/> component to the character, and storing optional configuration.
	/// </summary>
	/// <param name="character">The character Gameobject to attach the interaction to.</param>
	/// <param name="markerPoint">The marker point for the interaction's question mark UI.</param>
	/// <param name="interactionName">A unique name for the interaction.</param>
	/// <param name="exc">Optional extra configuration for the interaction setup.</param>
	/// <exception cref="ArgumentNullException">Thrown if character is null.</exception>
	/// <exception cref="ArgumentException">Thrown if interactionName is null, empty, or already used.</exception>
	public NPC(GameObject? character, GameObject? markerPoint, string interactionName, ExtraConfig exc = default) :
		this(character?.transform!, markerPoint?.transform!, interactionName, exc)
	{ }

	/// <summary>
	/// Initializes a new NPC instance by validating inputs, setting up the <seealso cref="Interaction"/>,
	/// attaching an <seealso cref="InteractableCharacter"/> component to the character, and storing optional configuration.
	/// </summary>
	/// <param name="character">The character GameObject to attach the interaction to.</param>
	/// <param name="markerOffset">Creates a new marker with an offset.</param>
	/// <param name="interactionName">A unique name for the interaction.</param>
	/// <param name="exc">Optional extra configuration for the interaction setup.</param>
	/// <exception cref="ArgumentNullException">Thrown if character is null.</exception>
	/// <exception cref="ArgumentException">Thrown if interactionName is null, empty, or already used.</exception>
	public NPC(GameObject? character, Vector3? markerOffset, string interactionName, ExtraConfig exc = default) :
		this(character?.transform, (Transform)null!, interactionName, exc)
	{
		GameObject markerPoint = new GameObject("MarkerPoint");
		markerPoint.transform.SetParent(character!.transform);
		markerPoint.transform.localPosition = markerOffset ?? new Vector3(0, 0, 0);
		interactionCharacter.questionMarkTarget = markerPoint.transform;
	}

	/// <summary>
	/// Initializes a new NPC instance by validating inputs, setting up the <seealso cref="Interaction"/>,
	/// attaching an <seealso cref="InteractableCharacter"/> component to the character, and storing optional configuration.
	/// </summary>
	/// <param name="character">The character transform to attach the interaction to.</param>
	/// <param name="markerOffset">Creates a new marker with an offset.</param>
	/// <param name="interactionName">A unique name for the interaction.</param>
	/// <param name="exc">Optional extra configuration for the interaction setup.</param>
	/// <exception cref="ArgumentNullException">Thrown if character is null.</exception>
	/// <exception cref="ArgumentException">Thrown if interactionName is null, empty, or already used.</exception>
	public NPC(Transform? character, Vector3? markerOffset, string interactionName, ExtraConfig exc = default) :
		this(character?.transform, (Transform)null!, interactionName, exc)
	{
		GameObject markerPoint = new GameObject("MarkerPoint");
		markerPoint.transform.SetParent(character);
		markerPoint.transform.localPosition = markerOffset ?? new Vector3(0, 0, 0);
		interactionCharacter.questionMarkTarget = markerPoint.transform;
	}

	/// <summary>
	/// Contains a list of every NPC.
	/// </summary>
	public static List<NPC> instances { get; private set; } = new();

	/// <summary>
	/// Contains a list of every character that is used in the dialog.
	/// </summary>
	public List<InterCharacter> characters { get; set; } = null!;

	/// <summary>
	/// The Interaction that is created for the NPC.
	/// </summary>
	public Interaction interaction { get; set; } = null!;

	/// <summary>
	/// The InteractableCharacter component that is attached to the NPC.
	/// </summary>
	public InteractableCharacter interactionCharacter { get; set; } = null!;

	/// <summary>
	/// Contains every line that the dialog uses.
	/// </summary>
	public List<InteractionLine> lines { get; set; } = null!;

	/// <summary>
	/// Indicates whether the NPC system has been initialized and IL patches applied.
	/// </summary>
	private static bool hasInit { get; set; } = false;

	/// <summary>
	/// Stores the extra configuration options for this NPC instance.
	/// </summary>
	private ExtraConfig _extraConfig { get; set; } = default;

	/// <summary>
	/// Ensures the OnComplete action is set for NPCs with persistent completion logic after dialog finishes.
	/// </summary>
	/// <param name="character">The <see cref="InteractableCharacter"/> to check and update.</param>
	public static void ShouldSkipOCN(InteractableCharacter character)
	{
		NPC? justRan = NPC.instances?.FirstOrDefault(npc => npc.interactionCharacter == character && npc._extraConfig.PersistantOnComplete && npc.interactionCharacter.onComplete == null);
		if (justRan != null)
		{ justRan.interactionCharacter.onComplete = (justRan._extraConfig.OnComplete != null ? () => { justRan._extraConfig.OnComplete?.Invoke(justRan); } : null); }
	}

	/// <summary>
	/// Commits the dialog to the NPC by doing checks, <seealso cref="InterCharacter"/> creation/obtaining and dialog validation.<br/>
	/// This also clears any previous dialogs that were commited.
	/// </summary>
	/// <param name="dialogs"></param>
	/// <exception cref="ArgumentException"></exception>
	public void CommitDialog(List<DialogEntry> dialogs)
	{
		// Null check
		if (dialogs == null) throw new ArgumentException("No dialog was provided.");

		// Creates a new empty list, discarding the original lines
		lines = new();

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
			InterCharacter character = characters.FirstOrDefault(c => c.Character == dialog.character);

			// If the character does not exist, we create a new one
			if (character.InteractionCharacter == null || string.IsNullOrEmpty(character.Name))
			{
				InteractionCharacter intactChar = ScriptableObject.CreateInstance<InteractionCharacter>();
				characters.Add(new(dialog.character, intactChar));
				character = characters.Last();

				intactChar.DisplayName = new UnlocalizedString(character.Name);
				intactChar.CharacterColor = new Color(0.3216f, 0.1137f, 0.2923f, 1); // Does not matter
				intactChar.TalkingSprite = GameObject.Find("Hub_Characters/Captain/").GetComponent<InteractableCharacter>().character.TalkingSprite;  // Does not matter
				intactChar.Ability = AbilityKind.BoardBoost; // Does not matter
			}

			InteractionLine interactionLine = new();
			interactionLine.line = new UnlocalizedString(dialog.line);
			interactionLine.character = character.InteractionCharacter;
			interactionLine.requirements = [];
			lines!.Add(interactionLine);
			interaction.Lines = lines.ToArray();
		}

		interactionCharacter.character = lines.First().character;

		if (!interactionCharacter.enabled)
		{
			interactionCharacter.enabled = true;
			_extraConfig.OnCreated?.Invoke(this);
		}
	}
}

/// <summary>
/// Represents a single dialog line entry, associating a character with a line of dialog.
/// </summary>
/// <param name="character">The character speaking the line.</param>
/// <param name="line">The dialog line text.</param>
public record DialogEntry(Characters character, string line);
