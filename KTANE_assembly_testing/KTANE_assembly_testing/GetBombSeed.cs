#pragma warning disable MSB3257
using UnityEngine;

public class GetBombSeed : MonoBehaviour
{
    public Bomb bomb = null;

    public int BombSeed()
    {
        bomb = GetBomb();
        return bomb.Seed;
    }

    public bool FoundBomb()
    {
        bomb = GetBomb();

        if (bomb == null)
        {
            return false;
        }
        else
        {
            return true;
        }
    }

    public Bomb GetBomb()
    {
        return GetComponentInParent<Bomb>();
    }
}