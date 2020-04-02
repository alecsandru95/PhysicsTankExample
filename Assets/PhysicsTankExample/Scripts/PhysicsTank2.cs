using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PhysicsTank2 : MonoBehaviour
{
	[SerializeField]
	private float _TopSpeed = 10;
	[SerializeField]
	private float _MotorTorque = 30;
	[SerializeField]
	private Transform _CenterOfMass;
	[SerializeField]
	private GameObject _WheelPrefab;

	private Rigidbody _Rigidbody;
	private Transform _Transform;

	private Dictionary<WheelCollider, Transform> _WheelTransformDictionary;
	private WheelCollider[] _WheelColliders;

	private float _ForwardInput = 0;
	private float _TurnInput = 0;

	private float _FinalForward;
	private float _FinalTurn;

	private float _ForwardSpeed;

	private Dictionary<WheelCollider, float> _SteeringAngleDictionary;

	private void Awake()
	{
		_Rigidbody = GetComponent<Rigidbody>();
		_Transform = GetComponent<Transform>();

		_WheelColliders = GetComponentsInChildren<WheelCollider>();

		_WheelTransformDictionary = new Dictionary<WheelCollider, Transform>(_WheelColliders.Length);
	}

	private void Start()
	{
		//sanity check
		Debug.Assert(_WheelPrefab != null);


		if (_CenterOfMass != null)
		{
			Debug.Assert(_CenterOfMass.parent == _Transform);
			_Rigidbody.centerOfMass = _CenterOfMass.localPosition;
		}

		foreach (var wheelCollider in _WheelColliders)
		{
			var newWheel = Instantiate(_WheelPrefab, wheelCollider.transform, false);
			newWheel.transform.localScale = Vector3.one * wheelCollider.radius * 2;

			_WheelTransformDictionary.Add(wheelCollider, newWheel.transform);
		}

		SetAngleDictionary();
	}

	private void Update()
	{
		_ForwardInput = Input.GetAxis("Vertical");
		_TurnInput = Input.GetAxis("Horizontal");

		UpdateWheelTransform();
	}

	private void FixedUpdate()
	{
		_ForwardSpeed = _Transform.InverseTransformDirection(_Rigidbody.velocity).z;
		if(_ForwardSpeed > _TopSpeed || _ForwardSpeed < - _TopSpeed * 0.25f)
		{
			_ForwardInput = 0;
		}

		MoveByInverseSteering();
	}

	private void MoveByInverseSteering()
	{
		_FinalTurn = _TurnInput / Mathf.Abs(_ForwardSpeed > 1 ? _ForwardSpeed : 1);

		_FinalForward = _ForwardInput * (1 - Mathf.Abs(_FinalTurn) * 0.75f);

		if (Mathf.Abs(_ForwardSpeed) < 1f && Mathf.Abs(_FinalTurn) > 0.1f)
		{
			_FinalForward = 0.2f * Mathf.Abs(_FinalTurn);
		}

		foreach (var wheelCollider in _WheelColliders)
		{
			var angle = _SteeringAngleDictionary[wheelCollider];

			if (Mathf.Abs(_ForwardSpeed) < 1f)
			{
				if (_TurnInput > 0)
				{
					angle += 90;
				}
				else if (_TurnInput < 0)
				{
					angle -= 90;
					angle = -angle;
				}
			}
			else
			{
				angle = Mathf.Abs(angle - Mathf.Sign(angle) * 90);

				if(wheelCollider.transform.localPosition.z < 0)
				{
					angle = -angle;
				}
			}

			wheelCollider.steerAngle = angle * _FinalTurn;

			wheelCollider.motorTorque = _MotorTorque * _FinalForward;
		}
	}
	private void MoveByInverseTorque()
	{
		_FinalTurn = _TurnInput / Mathf.Abs(_ForwardSpeed > 1 ? _ForwardSpeed : 1);
		_FinalForward = _ForwardInput * (1 - Mathf.Abs(_FinalTurn));
	}

	private void SetAngleDictionary()
	{
		if (_SteeringAngleDictionary == null)
		{
			_SteeringAngleDictionary = new Dictionary<WheelCollider, float>(_WheelColliders.Length);
		}

		foreach (var wheelCol in _WheelColliders)
		{
			var angle = Mathf.Atan2(wheelCol.transform.localPosition.x, wheelCol.transform.localPosition.z) * Mathf.Rad2Deg;

			if (angle > 180)
			{
				angle -= 360;
			}
			else if (angle < -180)
			{
				angle += 360;
			}

			Debug.Log($"Angle for wheel {wheelCol.name} is {angle} at position {wheelCol.transform.localPosition.ToString()}");


			_SteeringAngleDictionary.Add(wheelCol, angle);
		}
	}

	private void UpdateWheelTransform()
	{
		Vector3 newPosition;
		Quaternion newRotation;

		foreach (var wheelCollider in _WheelTransformDictionary.Keys)
		{
			wheelCollider.GetWorldPose(out newPosition, out newRotation);

			_WheelTransformDictionary[wheelCollider].position = newPosition;
			_WheelTransformDictionary[wheelCollider].rotation = newRotation;

			Debug.DrawRay(
				_WheelTransformDictionary[wheelCollider].position,
				wheelCollider.transform.localPosition.x > 0 ? -_WheelTransformDictionary[wheelCollider].right : _WheelTransformDictionary[wheelCollider].right,
				Color.red);
		}
	}

}
