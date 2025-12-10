using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace TMKOC.FuzzBugClone
{
    public class JarController : MonoBehaviour
    {
        [SerializeField] private Image _jar;



        private Material _jarMaterial;

        private void Start()
        {
            // Create runtime instance of the material
            _jarMaterial = _jar.materialForRendering;
        }



    }
}
