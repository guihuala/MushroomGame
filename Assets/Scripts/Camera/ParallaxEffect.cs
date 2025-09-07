using UnityEngine;

public class ParallaxEffect : MonoBehaviour
{
    [Header("视差设置")]
    public float parallaxSpeed = 0.5f;
    public bool infiniteHorizontal = true;
    public bool infiniteVertical = false;
    
    private Transform cameraTransform;
    private Vector3 lastCameraPosition;
    private float textureUnitSizeX;
    private float textureUnitSizeY;
    
    void Start()
    {
        cameraTransform = Camera.main.transform;
        lastCameraPosition = cameraTransform.position;
        
        Sprite sprite = GetComponent<SpriteRenderer>().sprite;
        Texture2D texture = sprite.texture;
        
        // 计算纹理单位大小
        textureUnitSizeX = texture.width / sprite.pixelsPerUnit;
        textureUnitSizeY = texture.height / sprite.pixelsPerUnit;
    }
    
    void LateUpdate()
    {
        Vector3 deltaMovement = cameraTransform.position - lastCameraPosition;
        transform.position += new Vector3(deltaMovement.x * parallaxSpeed, 
                                         deltaMovement.y * parallaxSpeed, 
                                         0);
        lastCameraPosition = cameraTransform.position;
        
        if (infiniteHorizontal)
        {
            if (Mathf.Abs(cameraTransform.position.x - transform.position.x) >= textureUnitSizeX)
            {
                float offsetPositionX = (cameraTransform.position.x - transform.position.x) % textureUnitSizeX;
                transform.position = new Vector3(cameraTransform.position.x + offsetPositionX, 
                                                transform.position.y, 
                                                transform.position.z);
            }
        }
        
        // 无限垂直滚动
        if (infiniteVertical)
        {
            if (Mathf.Abs(cameraTransform.position.y - transform.position.y) >= textureUnitSizeY)
            {
                float offsetPositionY = (cameraTransform.position.y - transform.position.y) % textureUnitSizeY;
                transform.position = new Vector3(transform.position.x, 
                                                cameraTransform.position.y + offsetPositionY, 
                                                transform.position.z);
            }
        }
    }
}