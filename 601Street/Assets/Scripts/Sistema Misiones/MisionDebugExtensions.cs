using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

// M�todo de extensi�n para el MisionManager
public static class MisionDebugExtensions
{
    /// <summary>
    /// Obtiene informaci�n sobre el origen de la llamada
    /// </summary>
    /// <returns>String con informaci�n del origen de la llamada</returns>
    public static string ObtenerOrigenLlamada()
    {
        // Obtener el stack trace actual
        StackTrace stackTrace = new StackTrace(true);

        // Intentamos encontrar el frame que contiene la llamada original
        // Normalmente ser�a el tercer frame (0 es este m�todo, 1 es el m�todo que lo llam� en MisionManager)
        int frameIndex = 2;

        // Si hay suficientes frames
        if (stackTrace.FrameCount > frameIndex)
        {
            StackFrame frame = stackTrace.GetFrame(frameIndex);
            string nombreMetodo = frame.GetMethod().Name;
            string nombreClase = frame.GetMethod().DeclaringType.Name;
            string nombreArchivo = frame.GetFileName();
            int lineaArchivo = frame.GetFileLineNumber();

            // Verificar si tenemos la informaci�n del archivo
            if (string.IsNullOrEmpty(nombreArchivo))
            {
                return $"M�todo: {nombreMetodo}, Clase: {nombreClase}";
            }
            else
            {
                // Obtener solo el nombre del archivo sin la ruta completa
                string[] partesRuta = nombreArchivo.Split('\\', '/');
                string nombreArchivoCorto = partesRuta[partesRuta.Length - 1];

                return $"M�todo: {nombreMetodo}, Clase: {nombreClase}, Archivo: {nombreArchivoCorto}, L�nea: {lineaArchivo}";
            }
        }

        // Si no hay suficientes frames
        return "No se pudo determinar el origen de la llamada";
    }
}