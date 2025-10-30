using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimatorRedirector : MonoBehaviour
{
    [SerializeField] private EnemyComponent _scriptOnOtherObject;

    public void CallStopAttack()
    {
        _scriptOnOtherObject.StopAttack();
    }

    public void CallStopDeathAnim()
    {
        _scriptOnOtherObject.StopDeathAnim();
    }
}
