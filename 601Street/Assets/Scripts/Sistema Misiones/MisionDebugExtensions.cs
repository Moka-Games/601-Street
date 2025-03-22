using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

// Método de extensión para el MisionManager
public static class MisionDebugExtensions
{
    /// <summary>
    /// Obtiene información sobre el origen de la llamada
    /// </summary>
    /// <returns>String con información del origen de la llamada</returns>
    public static string ObtenerOrigenLlamada()
    {
        // Obtener el stack trace actual
        StackTrace stackTrace = new StackTrace(true);

        // Intentamos encontrar el frame que contiene la llamada original
        // Normalmente sería el tercer frame (0 es este método, 1 es el método que lo llamó en MisionManager)
        int frameIndex = 2;

        // Si hay suficientes frames
        if (stackTrace.FrameCount > frameIndex)
        {
            StackFrame frame = stackTrace.GetFrame(frameIndex);
            string nombreMetodo = frame.GetMethod().Name;
            string nombreClase = frame.GetMethod().DeclaringType.Name;
            string nombreArchivo = frame.GetFileName();
            int lineaArchivo = frame.GetFileLineNumber();

            // Verificar si tenemos la información del archivo
            if (string.IsNullOrEmpty(nombreArchivo))
            {
                return $"Método: {nombreMetodo}, Clase: {nombreClase}";
            }
            else
            {
                // Obtener solo el nombre del archivo sin la ruta completa
                string[] partesRuta = nombreArchivo.Split('\\', '/');
                string nombreArchivoCorto = partesRuta[partesRuta.Length - 1];

                return $"Método: {nombreMetodo}, Clase: {nombreClase}, Archivo: {nombreArchivoCorto}, Línea: {lineaArchivo}";
            }
        }

        // Si no hay suficientes frames
        return "No se pudo determinar el origen de la llamada";
    }
}