using UnityEngine;
using System.Collections;

namespace Gameplay.Unit.Attack
{
    public interface IHitByBullet
    {
        void Hit(HitInformation hitInformation);
    }
}