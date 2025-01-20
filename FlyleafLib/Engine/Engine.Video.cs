﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using Vortice.DXGI;

using FlyleafLib.MediaFramework.MediaDevice;

namespace FlyleafLib;

public class VideoEngine
{
    /// <summary>
    /// List of Video Capture Devices
    /// </summary>
    public ObservableCollection<VideoDevice>
                            CapDevices          { get; set; } = new();
    public void             RefreshCapDevices() => VideoDevice.RefreshDevices();

    /// <summary>
    /// List of GPU Adpaters <see cref="Config.VideoConfig.GPUAdapter"/>
    /// </summary>
    public Dictionary<long, GPUAdapter>
                            GPUAdapters         { get; private set; }

    /// <summary>
    /// List of GPU Outputs from default GPU Adapter (Note: will no be updated on screen connect/disconnect)
    /// </summary>
    public List<GPUOutput>  Screens             { get; private set; } = new List<GPUOutput>();

    internal IDXGIFactory2  Factory;

    internal VideoEngine()
    {
        if (DXGI.CreateDXGIFactory1(out Factory).Failure)
            throw new InvalidOperationException("Cannot create IDXGIFactory1");

        GPUAdapters = GetAdapters();
    }

    private Dictionary<long, GPUAdapter> GetAdapters()
    {
        Dictionary<long, GPUAdapter> adapters = new();

        string dump = "";

        for (uint i=0; Factory.EnumAdapters1(i, out var adapter).Success; i++)
        {
            bool hasOutput = false;

            List<GPUOutput> outputs = new();

            int maxHeight = 0;
            for (uint o=0; adapter.EnumOutputs(o, out var output).Success; o++)
            {
                GPUOutput gpout = new()
                {
                    Id        = GPUOutput.GPUOutputIdGenerator++,
                    DeviceName= output.Description.DeviceName,
                    Left      = output.Description.DesktopCoordinates.Left,
                    Top       = output.Description.DesktopCoordinates.Top,
                    Right     = output.Description.DesktopCoordinates.Right,
                    Bottom    = output.Description.DesktopCoordinates.Bottom,
                    IsAttached= output.Description.AttachedToDesktop,
                    Rotation  = (int)output.Description.Rotation
                };

                if (maxHeight < gpout.Height)
                    maxHeight = gpout.Height;

                outputs.Add(gpout);

                if (gpout.IsAttached)
                    hasOutput = true;

                output.Dispose();
            }

            if (Screens.Count == 0 && outputs.Count > 0)
                Screens = outputs;

            adapters[adapter.Description1.Luid] = new GPUAdapter()
            {
                SystemMemory    = adapter.Description1.DedicatedSystemMemory.Value,
                VideoMemory     = adapter.Description1.DedicatedVideoMemory.Value,
                SharedMemory    = adapter.Description1.SharedSystemMemory.Value,
                Vendor          = VendorIdStr(adapter.Description1.VendorId),
                Description     = adapter.Description1.Description,
                Id              = adapter.Description1.DeviceId,
                Luid            = adapter.Description1.Luid,
                MaxHeight       = maxHeight,
                HasOutput       = hasOutput,
                Outputs         = outputs
            };

            dump += $"[#{i+1}] {adapters[adapter.Description1.Luid]}\r\n";

            adapter.Dispose();
        }

        Engine.Log.Info($"GPU Adapters\r\n{dump}");

        return adapters;
    }

    // Use instead System.Windows.Forms.Screen.FromPoint
    public GPUOutput GetScreenFromPosition(int top, int left)
    {
        foreach(var screen in Screens)
        {
            if (top >= screen.Top && top <= screen.Bottom && left >= screen.Left && left <= screen.Right)
                return screen;
        }

        return null;
    }

    private static string VendorIdStr(uint vendorId)
    {
        switch (vendorId)
        {
            case 0x1002:
                return "ATI";
            case 0x10DE:
                return "NVIDIA";
            case 0x1106:
                return "VIA";
            case 0x8086:
                return "Intel";
            case 0x5333:
                return "S3 Graphics";
            case 0x4D4F4351:
                return "Qualcomm";
            default:
                return "Unknown";
        }
    }
}
