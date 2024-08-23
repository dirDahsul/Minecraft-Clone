using UnityEngine;
using UnityEngine.InputSystem;

public class ObjectWireframeOutline : MonoBehaviour
{
    public LayerMask groundLayer;
    public float outlineDistance = 4f;
    public const float boxSize = 1.0f;

    public float outlineWidth = 4f;

    private GameObject boxContainer = null;

    private GameObject box = null;

    private void Start()
    {
        CreateWireframeBox();
    }

    private void Update()
    {
        //Ray ray = this.ScreenPointToRay(Mouse.current.position.ReadValue());
        RaycastHit hit;

        // Perform the raycast with a layer mask
        if (Physics.Raycast(transform.position, transform.forward, out hit, outlineDistance, groundLayer))
        {
            if(boxContainer != hit.collider.transform.parent.gameObject)
            {
                boxContainer = hit.collider.transform.parent.gameObject;
                //box.transform.parent = boxContainer.transform;
                box.transform.position = new Vector3(boxContainer.transform.position.x, boxContainer.transform.position.y, boxContainer.transform.position.z);
                box.SetActive(true);
                //isOutlined = true;
            }
        }
        else if (box.activeSelf)
        {
            // If no object is hit and the object is currently outlined, destroy the box
            box.SetActive(false);
            boxContainer = null;
            //isOutlined = false;
        }
    }

    private void CreateWireframeBox()
    {
        // Create the box GameObject
        box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = "WireframeBox";
        //box.transform.rotation = Quaternion.identity;
        box.transform.localScale = new Vector3(boxSize, boxSize, boxSize);
        //box.transform.localPosition = new Vector3(0,0,0);
        //box.layer = 2; ????

        // Remove the collider from the box
        Destroy(box.GetComponent<Collider>());
        //Destroy(box.GetComponent<Renderer>());
        //Destroy(box.GetComponent<MeshFilter>());

        // Create a new material with a wireframe shader
        /*Material wireframeMaterial = new Material(Shader.Find("Hidden/Internal-Colored"));

        // Set the wireframe material properties
        wireframeMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        wireframeMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        wireframeMaterial.SetInt("_ZWrite", 0);
        wireframeMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
        wireframeMaterial.SetInt("_ColorMask", (int)UnityEngine.Rendering.ColorWriteMask.All);
        wireframeMaterial.SetInt("_BlendOp", (int)UnityEngine.Rendering.BlendOp.Add);
        wireframeMaterial.SetFloat("_LineWidth", 2f);

        // Set the wireframe material color
        wireframeMaterial.SetColor("_Color", Color.white);

        // Assign the wireframe material to the box
        MeshRenderer renderer = box.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = wireframeMaterial;

        // Disable shadows for the box
        MeshRenderer renderer = box.GetComponent<MeshRenderer>();
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;*/

        // Get the renderer component attached to the box
        Renderer renderer = box.GetComponent<Renderer>();

        // Create a new material with a transparent shader
        Material transparentMaterial = new Material(Shader.Find("Transparent/Diffuse"));

        // Set the transparency level
        Color color = renderer.material.color;
        color.a = 0f; // Set the alpha value between 0 (fully transparent) and 1 (fully opaque)
        transparentMaterial.color = color;

        // Assign the transparent material to the renderer
        renderer.material = transparentMaterial;

        box.AddComponent<Outline>().OutlineWidth = outlineWidth;
        //box.GetComponent<Outline>().OutlineColor = Color.white;
        //box.GetComponent<Outline>().OutlineWidth = 4;
        box.SetActive(false);
    }
}