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

public struct ExtraConfig
{
	/// <summary>
	/// Set's a custom camera rig for the NPC.
	/// </summary>
	public InteractionCameraRig cameraRig;

	/// <summary>
	/// Invoke action when the NPC dialog is finished
	/// </summary>
	public Action<NPC> onComplete;

	/// <summary>
	/// Invoke action when the NPC is created.
	/// </summary>
	public Action<NPC> onCreated;

	public ExtraConfig()
	{
		cameraRig = null!;
		onComplete = null!;
		onCreated = null!;
	}
}

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
			Characters.Grunt | Characters.Grunt2 | Characters.Grunt3 => "Grunt",
			Characters.Weeboh | Characters.Weeboh2 | Characters.Weeboh3 => "Weeboh",
			_ => "Captain",
		};

		InteractionCharacter = interactonCharacter;
		InteractionCharacter.CharacterSprite = GetCharacterSprite();
		InteractionCharacter.VocalBank = GetCharacterVocals(Name);
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

	private Sprite GetCharacterSprite()
	{
		return GameObject.Find($"GAME/Handlers/InteractionHandler/InteractionUI/Canvas/Characters/{Character.ToString()}/Image")
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

	/// <summary>
	/// Adds a new dialog entry to the list.
	/// </summary>
	/// <param name="c"></param>
	/// <param name="l"></param>
	public void Add(Characters c, string l) => Add(new DialogEntry(c, l));

	/// <summary>
	/// Calls the <see cref="NPC.CommitDialog"/> method once the using is finished.
	/// Making it simplier for the user to create a dialog.
	/// </summary>
	public void Dispose() => _npc.CommitDialog(this);
}

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
	public NPC(Transform character, Transform markerPoint, string interactionName, ExtraConfig exc = default)
	{
		if (character == null)
		{ throw new ArgumentNullException(nameof(character), "Character is null."); }

		if (string.IsNullOrEmpty(interactionName))
		{ throw new ArgumentException("Interaction name is null or empty.", nameof(interactionName)); }

		if (instances.Any(i => i.interaction.name == interactionName))
		{ throw new ArgumentException($"Interaction name '{interactionName}' already exists.", nameof(interactionName)); }

		if (exc.cameraRig == null)
		{ exc.cameraRig = GameObject.Find("Hub_Characters/CaptainCameraRig").GetComponent<InteractionCameraRig>(); }

		// Setup the new interaction
		interaction.name = interactionName;
		interaction.factsToSet = [];
		interaction.canBePlayedSeveralTimes = true;

		// Create a new InteractableCharacter component and add it to the Character gameobject.
		// Additionally we turn it off straight away so the Start() method does not execute.
		(interactionCharacter = character.gameObject.AddComponent<InteractableCharacter>()).enabled = false;
		interactionCharacter.questionMarkTarget = markerPoint;
		interactionCharacter.interactionCenter = character;
		interactionCharacter.unlockInteraction = interaction;
		interactionCharacter.cameraRig = exc.cameraRig;
		interactionCharacter.onComplete = (exc.onComplete != null ? () => { exc.onComplete?.Invoke(this); } : null);

		_extraConfig = exc;
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
	public NPC(GameObject character, Transform markerPoint, string interactionName, ExtraConfig exc = default) :
		this(character.transform, markerPoint, interactionName, exc)
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
	public NPC(Transform character, GameObject markerPoint, string interactionName, ExtraConfig exc = default) :
		this(character, markerPoint.transform, interactionName, exc)
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
	public NPC(GameObject character, GameObject markerPoint, string interactionName, ExtraConfig exc = default) :
		this(character.transform, markerPoint.transform, interactionName, exc)
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
	public NPC(GameObject character, Vector3 markerOffset, string interactionName, ExtraConfig exc = default) :
		this(character.transform, (GameObject)null!, interactionName, exc)
	{
		GameObject markerPoint = new GameObject("MarkerPoint");
		markerPoint.transform.SetParent(character.transform);
		markerPoint.transform.localPosition = markerOffset;
		interactionCharacter.questionMarkTarget = markerPoint.transform;
	}

	/// <summary>
	/// Contains a list of every NPC.
	/// </summary>
	public static List<NPC> instances { get; set; } = new();

	/// <summary>
	/// Contains a list of every character that is used in the dialog.
	/// </summary>
	public List<InterCharacter> characters { get; set; } = new();

	/// <summary>
	/// The Interaction that is created for the NPC.
	/// </summary>
	public Interaction interaction { get; set; } = new();

	/// <summary>
	/// The InteractableCharacter component that is attached to the NPC.
	/// </summary>
	public InteractableCharacter interactionCharacter { get; set; } = null!;

	/// <summary>
	/// Contains every line that the dialog uses.
	/// </summary>
	public List<InteractionLine> lines { get; set; } = new();

	/// <summary>
	/// Stores extra configuration for the NPC.
	/// </summary>
	private ExtraConfig _extraConfig { get; set; } = default;

	/// <summary>
	/// Commits the dialog to the NPC by doing checks, <seealso cref="InterCharacter"/> creation/obtaining and dialog validation.
	/// </summary>
	/// <param name="dialogs"></param>
	/// <exception cref="ArgumentException"></exception>
	public void CommitDialog(List<DialogEntry> dialogs)
	{
		// Null check
		if (dialogs == null) throw new ArgumentException("No dialog was provided.");

		lines?.Clear();

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
				InteractionCharacter intactChar = new();
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
		UnityEngine.Debug.LogError("13");

		if (!interactionCharacter.enabled)
		{
			interactionCharacter.enabled = true;
			_extraConfig.onCreated?.Invoke(this);
		}
	}
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

public record DialogEntry(Characters character, string line);

public static class ILPatching
{
	/// <summary>
	/// Overrides the Start and Awake methods of the <seealso cref="InteractableCharacter"> class that is inherited from the <seealso cref="MonoBehaviour"/> class."
	/// </summary>
	/// <exception cref="Exception"></exception>
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
