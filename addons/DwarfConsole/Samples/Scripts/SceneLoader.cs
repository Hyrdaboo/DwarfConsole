using Godot;

public partial class SceneLoader : Area3D
{
    [Export]
	private string path;

    public override void _Ready()
    {
        BodyEntered += OnBodyEnter;
    }

    private void OnBodyEnter(Node node)
    {
        if (node.IsInGroup("Player"))
        {
            CallDeferred(nameof(LoadScene));
        }
    }

    private void LoadScene()
    {
        GetTree().ChangeSceneToFile(path);
    }
}
