using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Tutorial/Sequence", fileName = "TutorialSequence")]
public class TutorialSequence : ScriptableObject
{
    public List<TutorialStep> steps = new List<TutorialStep>();
}