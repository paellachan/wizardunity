using Naninovel.Commands;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Advertisements;


[CommandAlias("ads")]
public class ads : Command
{

    public override Task ExecuteAsync ()
    {
        Debug.Log("Hello World!");
		if (Advertisement.IsReady("rewardedVideo"))
        {
            Advertisement.Show("rewardedVideo");
        }
		
        return Task.CompletedTask;
    }

    public override Task UndoAsync () => Task.CompletedTask;
}
