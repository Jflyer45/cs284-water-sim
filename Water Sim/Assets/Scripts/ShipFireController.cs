using UnityEngine;
using System.Collections.Generic;

public class ShipFireController : MonoBehaviour
{
    private List<Cannon> cannons = new List<Cannon>();

    void Start()
    {
        cannons.Clear();
        Cannon[] foundCannons = GetComponentsInChildren<Cannon>(true);
        foreach (Cannon cannon in foundCannons)
        {
            cannons.Add(cannon);
        }
    }

    [ContextMenu("Fire ALL")]
    public void FireAllCannons()
    {
        foreach (Cannon cannon in cannons)
        {
            cannon.FireWithDelayPublic();
        }
    }
}