using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class Dash : MonoBehaviour
{
    public float distanceDash = 6f;    // Distance parcourue pendant le dash (en mètres)
    public float dureeDash = 0.15f;    // Durée du dash (en secondes)
    public float delaiRecharge = 1f;   // Temps d'attente entre deux dashs

    CharacterController cc;
    Vector3 directionDash;
    float tempsRestantDash;
    float prochainDashPossible;

    void Start()
    {
        cc = GetComponent<CharacterController>();
    }

    void Update()
    {
        // Clic droit = dash (si la recharge est terminée)
        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame
            && Time.time >= prochainDashPossible)
        {
            // Direction vers l'avant du joueur, à l'horizontale
            directionDash = transform.forward;
            directionDash.y = 0f;
            directionDash.Normalize();

            tempsRestantDash = dureeDash;
            prochainDashPossible = Time.time + delaiRecharge;
        }

        // Pendant le dash, on pousse le joueur vers l'avant
        if (tempsRestantDash > 0f)
        {
            float vitesse = distanceDash / dureeDash;
            cc.Move(directionDash * vitesse * Time.deltaTime);
            tempsRestantDash -= Time.deltaTime;
        }
    }
}
