using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;

public class MyCustomUI : UnityCommon.ScriptableUIBehaviour, Naninovel.UI.IManagedUI
{
	public Task InitializeAsync () => Task.CompletedTask;
}
 