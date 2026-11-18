using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public sealed class GoalTrigger : MonoBehaviour
{
    [SerializeField] private PlayerTeam team;
    public PlayerTeam Team => team;

    public void SetTeam(PlayerTeam newTeam)
    {
        team = newTeam;
    }
}
