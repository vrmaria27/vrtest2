using UnityEngine;

namespace Chelsea.Runtime
{
    public enum ExperienceMenuAction
    {
        Return,
        RotateLeft,
        RotateRight,
        Smaller,
        Larger,
        Day,
        Overcast,
        Dusk,
        Sources
    }

    /// <summary>A controller/hand adapter invokes this same action target.</summary>
    public sealed class ExperienceActionTarget : MonoBehaviour
    {
        [SerializeField] private ChelseaExperience experience;
        [SerializeField] private ExperienceMenuAction action;

        public void Configure(ChelseaExperience owner, ExperienceMenuAction menuAction)
        {
            experience = owner;
            action = menuAction;
        }

        public void Activate()
        {
            if (experience != null) experience.Activate(action);
        }
    }

    /// <summary>Selection only. This collider grants no walking or teleport permission.</summary>
    public sealed class ExperienceSelectionTarget : MonoBehaviour
    {
        [SerializeField] private ChelseaExperience experience;
        [SerializeField] private string sourceId;
        public string SourceId => sourceId;

        public void Configure(ChelseaExperience owner, string id)
        {
            experience = owner;
            sourceId = id;
        }

        public void Select()
        {
            if (experience != null) experience.SelectSource(sourceId);
        }
    }
}
