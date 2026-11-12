using System;
using UnityEngine;

public class RagdollController : MonoBehaviour, IInteractable
{
    [SerializeField] private HudMessage interactionPrompt = HudMessage.Push;
    [SerializeField] private Rigidbody[] bones;
    [SerializeField] private Rigidbody rootBone;
    [SerializeField] private Collider boundingBox;
    [SerializeField] private float pushForce = 5f;

    private bool activated;

    public HudMessage GetInteractionPrompt(PlayerInteractor player)
    {
        return activated ? HudMessage.None : interactionPrompt;
    }

    public bool TryInteract(PlayerInteractor player)
    {
        if (activated)
            return false;

        activated = true;

        foreach (Rigidbody bone in bones)
        {
            if (bone != null)
            {
                bone.isKinematic = false;
            }
        }

        Destroy(boundingBox.gameObject);

        Vector3 pushDirection = player.transform.forward;
        rootBone.AddForce(pushDirection * pushForce, ForceMode.Impulse);

        return true;
    }
}
