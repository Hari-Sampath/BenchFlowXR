using System.Collections;
using UnityEngine;

namespace BenchFlow
{
    public enum WorkbenchButtonAction
    {
        Run,
        Reset,
        Undo,
        StartLesson
    }

    /// <summary>
    /// Physical 3D button actuator for the workbench console.
    /// Provides physical push/depress animation, click audio, and triggers workbench commands.
    /// </summary>
    public sealed class WorkbenchButton : MonoBehaviour
    {
        public WorkbenchButtonAction actionType = WorkbenchButtonAction.Run;
        public Transform movingCap;
        public TutorialController tutorial;
        public WorkbenchState workbench;

        Vector3 originalLocalPos;
        bool isAnimating = false;

        void Awake()
        {
            if (movingCap == null)
            {
                Transform cap = transform.Find("BtnCap") ??
                                transform.Find("ResetCap") ??
                                transform.Find("UndoCap") ??
                                transform.Find("Cap");
                if (cap != null) movingCap = cap;
                else movingCap = transform;
            }

            if (movingCap != null)
            {
                originalLocalPos = movingCap.localPosition;
            }

            if (tutorial == null) tutorial = FindAnyObjectByType<TutorialController>();
            if (workbench == null) workbench = FindAnyObjectByType<WorkbenchState>();
        }

        /// <summary>
        /// Actuates the button: physically presses down, clicks, and executes action.
        /// </summary>
        public void Press()
        {
            if (movingCap != null && gameObject.activeInHierarchy && !isAnimating)
            {
                StartCoroutine(AnimatePress());
            }

            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayButton(transform.position);
            }

            ExecuteAction();
        }

        void ExecuteAction()
        {
            if (tutorial == null) tutorial = FindAnyObjectByType<TutorialController>();
            if (workbench == null) workbench = FindAnyObjectByType<WorkbenchState>();

            switch (actionType)
            {
                case WorkbenchButtonAction.Run:
                    if (tutorial != null) tutorial.OnRunButtonPressed();
                    break;
                case WorkbenchButtonAction.Reset:
                    if (workbench != null) workbench.ResetWorkbench();
                    else if (tutorial != null) tutorial.ResetTutorial();
                    break;
                case WorkbenchButtonAction.Undo:
                    if (workbench != null) workbench.UndoLastWire();
                    break;
                case WorkbenchButtonAction.StartLesson:
                    if (tutorial != null) tutorial.BeginLesson();
                    break;
            }
        }

        IEnumerator AnimatePress()
        {
            isAnimating = true;
            Vector3 downPos = originalLocalPos - Vector3.up * 0.0035f;

            float t = 0f;
            while (t < 0.06f)
            {
                t += Time.deltaTime;
                movingCap.localPosition = Vector3.Lerp(originalLocalPos, downPos, t / 0.06f);
                yield return null;
            }

            t = 0f;
            while (t < 0.08f)
            {
                t += Time.deltaTime;
                movingCap.localPosition = Vector3.Lerp(downPos, originalLocalPos, t / 0.08f);
                yield return null;
            }

            movingCap.localPosition = originalLocalPos;
            isAnimating = false;
        }
    }
}
