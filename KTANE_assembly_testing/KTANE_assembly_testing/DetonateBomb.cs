using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class DetonateBomb : MonoBehaviour
{
    protected Bomb bomb = null;

    public Bomb GetBomb()
    {
        return GetComponentInParent<Bomb>();
    }

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.D))
        {
            bomb = GetBomb();
            bomb.Detonate();
        }
    }
}

