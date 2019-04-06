using System;
using System.Net.NetworkInformation;
using ABUS.DeviceConnectivity.Messages.Camera;
using FluentAssertions;
using Xunit;

namespace ABUS.DeviceConnectivity.Tests
{
    public class NetworkSettingsTest
    {
        [Fact]
        public void NetworkSettingsShouldThrowIfIPsNotValid()
        {
            Action action = () => new NetworkSettings("", "10.0.0.1", "10.0.0.1", "10.0.0.1", "10.0.0.258");
            action.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void NetworkSettingsShouldNotThrowIfIPsValid()
        {
            Action action = () => new NetworkSettings("", "10.0.0.1", "10.0.0.1", "10.0.0.1", "10.0.0.0");
            action.Should().NotThrow<ArgumentException>();
        }

        [Fact]
        public void NetworkSettingsExtenderShouldReturnValidSettings()
        {
            Action action = () => NetworkInterface.GetAllNetworkInterfaces().GetNetworkInterface().GetNetworkSettings();
            action.Should().NotThrow<ArgumentException>();
        }
    }
}
