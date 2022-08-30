using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PopSorting : MonoBehaviour
{
    private void Start()
    {
        int[] array = new int[] { 1, 123, 12, 45, 12, 1, 33, 71, 8, 2, 3, 61, 73, 81, 124 };

        for (int i = 0; i < array.Length; i++)
        {
            for (int j = 0; j < array.Length - 1 - i; j++)
            {
                if (array[j] > array[j + 1])
                {
                    int temp = array[j];
                    array[j] = array[j + 1];
                    array[j + 1] = temp;
                }
            }
        }

        foreach (var item in array)
        {
            Debug.Log(item);
        }
    }
}
