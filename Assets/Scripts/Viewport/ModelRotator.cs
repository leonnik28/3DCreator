using UnityEngine;

/// <summary>
/// Вращает 3D-объект по горизонтали или вертикали.
/// </summary>
public class ModelRotator : MonoBehaviour
{
    public enum Axis { Horizontal, Vertical }

    private Transform _target;
    [SerializeField] private Axis _axis = Axis.Horizontal;
    [SerializeField] private float _speed = 120f;
    [SerializeField] private bool _reverse;

    private bool _isAnimating;

    public bool IsAnimating => _isAnimating;
    public Axis CurrentAxis => _axis;
    public float Speed { get => _speed; set => _speed = Mathf.Max(0.1f, value); }

    private void Awake()
    {
        if (_target == null)
            _target = transform;
    }

    private void Update()
    {
        if (!_isAnimating || _target == null) return;

        float angle = _speed * Time.deltaTime * (_reverse ? -1 : 1);
        if (_axis == Axis.Horizontal)
            _target.Rotate(Vector3.up, angle);
        else
            _target.Rotate(Vector3.right, angle);
    }

    public void SetTarget(Transform target) => _target = target;
    public void SetAxis(Axis axis) => _axis = axis;
    public void SetReverse(bool reverse) => _reverse = reverse;
    public void SetAnimating(bool animating) => _isAnimating = animating;
}
