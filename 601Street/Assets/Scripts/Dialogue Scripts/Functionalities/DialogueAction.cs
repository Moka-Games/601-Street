using System;
using UnityEngine;
public class DialogueAction
{
    public Action OnDefaultAction { get; set; }
    public Action OnSuccessAction { get; set; }
    public Action OnFailAction { get; set; }

    public DialogueAction(Action defaultAction, Action successAction, Action failAction)
    {
        OnDefaultAction = defaultAction;
        OnSuccessAction = successAction;
        OnFailAction = failAction;
    }

    public void Execute(bool? isSuccess = null)
    {
        if (!isSuccess.HasValue)
        {
            OnDefaultAction?.Invoke();
        }
        else if (isSuccess.Value)
        {
            OnSuccessAction?.Invoke();
        }
        else
        {
            OnFailAction?.Invoke();
        }
    }
}
