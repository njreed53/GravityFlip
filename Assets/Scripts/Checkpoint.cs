using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    private static Checkpoint currentActive; // tracks active one

    private Renderer rend;

    void Start()
    {
        rend = GetComponent<Renderer>();
        Deactivate();
    }

    public void Activate()
    {
        // Turn off previous checkpoint
        if (currentActive != null && currentActive != this)
        {
            currentActive.Deactivate();
        }

        currentActive = this;

        rend.material.color = Color.green;
    }

    void Deactivate()
    {
        rend.material.color = Color.yellow;
    }
}