using NaughtyAttributes;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AI;
using Zenject;
using UnityEngine.Events;

namespace AI
{
    public class HammerRobotBrain : MonoBehaviour
    {
        [HideInInspector] public UnityEvent<int> OnAttack;

        [Required, SerializeField] private NavMeshAgent _navMeshAgent;
        [Required, SerializeField] private Health _robotHealth;

        [SerializeField] private RobotVision _robotVision;

        [BoxGroup("Idle")]
        [SerializeField, MinMaxSlider(0.0f, 15.0f)] private Vector2 _idleDelayRange;

        [SerializeField, BoxGroup("Patrolling")] private Transform[] _patrolPoints;
        [SerializeField, BoxGroup("Patrolling")] private float _patrolSpeed;

        [SerializeField, BoxGroup("Chasing")] private float _chaseSpeed;
        [SerializeField, BoxGroup("Chasing")] private float _chaseMinDistance;
        [SerializeField, BoxGroup("Chasing")] private float _chaseMaxDistance;

        [SerializeField, BoxGroup("Combat")] private float _maxCombatDistance;
        [SerializeField, SOInheritedFrom(typeof(IRobotAttack)), BoxGroup("Combat")] private List<ScriptableObject> _robotAttacks = new List<ScriptableObject>(); // SOInheritedFrom attribute ensures that objects will inherit from IRobotAttack

        [Inject] private Player _player;

        private StateMachine _stateMachine;
        private RobotState_Delay _delayState;
        private RobotState_Patrol _patrolState;
        private RobotState_Chase _chaseState;
        private RobotState_Combat _combatState;

        private void Awake()
        {
            _stateMachine = new StateMachine();

            SetupStates();

            SetupTransitions();

            _stateMachine.SetState(_patrolState);

            _robotHealth.OnDeath.AddListener(() => Destroy(gameObject));
        }

        private void Update()
        {
            _stateMachine.Tick();
        }

        private void SetupStates()
        {
            _delayState = new RobotState_Delay(_idleDelayRange);
            _patrolState = new RobotState_Patrol(_patrolSpeed, _navMeshAgent, _patrolPoints);
            _chaseState = new RobotState_Chase(_robotVision, _navMeshAgent, _chaseSpeed, _chaseMinDistance, _chaseMaxDistance);

            List<IRobotAttack> attacks = _robotAttacks.ConvertAll(x => x as IRobotAttack);

            _combatState = new RobotState_Combat(_player, this, _maxCombatDistance, attacks);

            _combatState.OnPerformAttack += (index) => OnAttack?.Invoke(index);
        }

        private void SetupTransitions()
        {
            _stateMachine.AddTransition(_patrolState, _delayState, () => _patrolState.IsDone());
            _stateMachine.AddTransition(_delayState, _patrolState, () => _delayState.IsDone());

            _stateMachine.AddTransition(_delayState, _chaseState, () => _robotVision.IsVisible());
            _stateMachine.AddTransition(_patrolState, _chaseState, () => _robotVision.IsVisible());

            _stateMachine.AddTransition(_chaseState, _combatState, () => _chaseState.IsDone());
            _stateMachine.AddTransition(_chaseState, _delayState, () => _chaseState.IsLostPlayer());

            _stateMachine.AddTransition(_combatState, _delayState, () => _combatState.IsDone());
            _stateMachine.AddTransition(_combatState, _chaseState, () => _combatState.IsLostPlayer());
        }

        private void OnDrawGizmosSelected()
        {
            _robotVision.OnGizmos();
        }
    }
}
