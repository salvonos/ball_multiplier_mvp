using UnityEngine;

public enum GameMode
{
    Edit,
    Play
}

public class GameModeController : MonoBehaviour
{
    public static GameModeController Instance { get; private set; }

    [SerializeField] private GameMode mode = GameMode.Edit;
    [SerializeField] private BallSpawner spawner;

    public GameMode Mode => mode;

    private void Awake()
    {
        Instance = this;
    }

    public void EnterEditMode()
    {
        mode = GameMode.Edit;
        if (spawner != null)
            spawner.ClearBalls();
    }

    public void EnterPlayMode()
    {
        mode = GameMode.Play;
    }

    public void DropBalls()
    {
        mode = GameMode.Play;
        if (spawner != null)
            spawner.DropBalls();
    }

    public void ResetBalls()
    {
        if (spawner != null)
            spawner.ClearBalls();

        mode = GameMode.Edit;
    }
}
