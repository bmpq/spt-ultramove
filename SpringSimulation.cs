using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ultramove
{
    public class SpringSimulation
    {
        public float Position { get; private set; }
        public float TargetPosition { get; set; }
        public float Velocity { get; private set; }

        public float SpringConstant { get; set; } = 150f;
        public float Damping { get; set; } = 30f;
        public float Mass { get; set; } = 1f;

        public SpringSimulation(float startPosition, float targetPosition)
        {
            Position = startPosition;
            TargetPosition = targetPosition;
            Velocity = 0f;
        }

        public void AddForce(float force)
        {
            float acceleration = force / Mass;
            Velocity += acceleration;
        }

        public void Tick(float deltaTime)
        {
            float displacement = Position - TargetPosition;
            float springForce = -SpringConstant * displacement;
            float dampingForce = -Damping * Velocity;
            float netForce = springForce + dampingForce;
            float acceleration = netForce / Mass;

            Velocity += acceleration * deltaTime;
            Position += Velocity * deltaTime;
        }

        public void OverrideCurrent(float current)
        {
            Position = current;
            Velocity = 10f;
        }
    }
}
