using System;
using UnityEngine;

public interface IInteractable
{
    void Interact();
}

public class TriggerNote : MonoBehaviour, IInteractable
{
    [Header("Настройка состояния")]
    [SerializeField] private bool isArmed= false;

    [Header("Настройка реакции")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private GameObject trapVisualEffect;


    public void Interact()
    {
        if (isArmed)
        {
            Debug.Log("Триггер активен, повторные нажатия игнорируются");
            return;
        }

        isArmed = true;
        Debug.Log($"{gameObject.name} Записку подняли");

        if (trapVisualEffect != null ) trapVisualEffect.SetActive(true);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        if (isArmed)
        {
            ExecuteNote();
        }
        else
        {
            Debug.Log($"[{gameObject.name}] Триггер не взведен");
        }

        
    }

    private void ExecuteNote()
    {
        Debug.Log($"[{gameObject.name}] Срабатывание");

        isArmed = false;
        if (trapVisualEffect != null) trapVisualEffect.SetActive(false);
    }
}
