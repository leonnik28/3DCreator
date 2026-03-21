using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Камера вращается вокруг цели при перетаскивании мышью.
/// </summary>
public class OrbitCameraController : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform _target;

    [Header("Settings")]
    [SerializeField] private float _distance = 8f;
    [SerializeField] private float _minDistance = 2f;
    [SerializeField] private float _maxDistance = 20f;
    [SerializeField] private float _sensitivityX = 4f;
    [SerializeField] private float _sensitivityY = 2f;
    [SerializeField] private float _minY = -20f;
    [SerializeField] private float _maxY = 80f;
    [SerializeField] private float _zoomSpeed = 2f;

    private float _angleX;
    private float _angleY;
    private bool _isDragging;

    public Transform Target { get => _target; set => _target = value; }
    public float Distance { get => _distance; set => _distance = Mathf.Clamp(value, _minDistance, _maxDistance); }

    private void Start()
    {
        if (_target == null)
        {
            var modelMgr = FindObjectOfType<ModelManager>();
            if (modelMgr != null && modelMgr.CurrentModel != null)
                _target = modelMgr.CurrentModel.transform;
        }

        ApplyOrbit();
    }

    private void LateUpdate()
    {
        if (_target == null) return;

        if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
            _isDragging = true;
        if (Input.GetMouseButtonUp(0))
            _isDragging = false;

        if (_isDragging)
        {
            _angleX += Input.GetAxis("Mouse X") * _sensitivityX;
            _angleY -= Input.GetAxis("Mouse Y") * _sensitivityY;
            _angleY = Mathf.Clamp(_angleY, _minY, _maxY);
        }

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
            _distance = Mathf.Clamp(_distance - scroll * _zoomSpeed * 10f, _minDistance, _maxDistance);

        ApplyOrbit();
    }

    private void ApplyOrbit()
    {
        if (_target == null) return;

        var rot = Quaternion.Euler(_angleY, _angleX, 0);
        var pos = _target.position - rot * Vector3.forward * _distance;
        transform.position = pos;
        transform.LookAt(_target.position);
    }

    public void SetTarget(Transform target)
    {
        _target = target;
        ApplyOrbit();
    }

}
