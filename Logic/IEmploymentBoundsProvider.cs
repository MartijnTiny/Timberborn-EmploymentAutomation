using UnityEngine;

namespace EmploymentAutomation.Logic;

public interface IEmploymentBoundsProvider
{
    public bool Available { get; }
    public bool Active { get; set; }
    public float High { get; set; }
    public float Low { get; set; }
    public float Fillrate { get; }
    public Vector2Int EmploymentBounds { get; }
}