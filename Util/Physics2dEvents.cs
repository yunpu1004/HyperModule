using System;
using UnityEngine;

namespace HyperModule
{
    public class Physics2dEvents : MonoBehaviour
    {
        public Collider2D col {get; private set;}
        public Action<Collider2D> OnTriggerEnter2dEvent {get; set;}
        public Action<Collider2D> OnTriggerStay2dEvent {get; set;} 
        public Action<Collider2D> OnTriggerExit2dEvent {get; set;}

        public Action<Collision2D> OnCollisionEnter2dEvent {get; set;}
        public Action<Collision2D> OnCollisionStay2dEvent {get; set;}
        public Action<Collision2D> OnCollisionExit2dEvent {get; set;}

        private void Awake()
        {
            col = GetComponent<Collider2D>();
        }

        private void OnTriggerEnter2D(Collider2D other) 
        {
            OnTriggerEnter2dEvent?.Invoke(other);
        }

        private void OnTriggerStay2D(Collider2D other) 
        {
            OnTriggerStay2dEvent?.Invoke(other);
        }

        private void OnTriggerExit2D(Collider2D other) 
        {
            OnTriggerExit2dEvent?.Invoke(other);
        }

        private void OnCollisionEnter2D(Collision2D other) 
        {
            OnCollisionEnter2dEvent?.Invoke(other);
        }

        private void OnCollisionStay2D(Collision2D other) 
        {
            OnCollisionStay2dEvent?.Invoke(other);
        }

        private void OnCollisionExit2D(Collision2D other) 
        {
            OnCollisionExit2dEvent?.Invoke(other);
        }
    }
}