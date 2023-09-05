using UnityEngine;
using Interfaces;

public class MedKit : MonoBehaviour, ITriggerableMonoBehaviour
{
    public float restorationAmount = 20;

    public void Trigger(Transform obj)
    {
        var health = obj.GetComponent<Health>();
        if (health == null) 
            return;

        if (health.Current != health.Max)
        {
            health.Heal(restorationAmount);
            gameObject.SetActive(false);
        }        
    }
}
