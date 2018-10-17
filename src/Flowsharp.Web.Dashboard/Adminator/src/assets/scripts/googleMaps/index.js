import * as $ from 'jquery';
import loadGoogleMapsAPI  from 'load-google-maps-api';

export default (function () {
  if ($('#google-map').length > 0) {
    loadGoogleMapsAPI({
      key: 'AIzaSyDW8td30_gj6sGXjiMU0ALeMu1SDEwUnEA',
    }).then(() => {
      const latitude  = 26.8206;
      const longitude = 30.8025;
      const mapZoom   = 5;
      const { google }    = window;

      const mapOptions = {
        center    : new google.maps.LatLng(latitude, longitude),
        zoom      : mapZoom,
        mapTypeId : google.maps.MapTypeId.ROADMAP,
        styles: [{
          'featureType': 'landscape',
          'stylers': [
            { 'hue'        : '#FFBB00' },
            { 'saturation' : 43.400000000000006 },
            { 'lightness'  : 37.599999999999994 },
            { 'gamma'      : 1 },
          ],
        }, {
          'featureType': 'road.highway',
          'stylers': [
            { 'hue'        : '#FFC200' },
            { 'saturation' : -61.8 },
            { 'lightness'  : 45.599999999999994 },
            { 'gamma'      : 1 },
          ],
        }, {
          'featureType': 'road.arterial',
          'stylers': [
            { 'hue'        : '#FF0300' },
            { 'saturation' : -100 },
            { 'lightness'  : 51.19999999999999 },
            { 'gamma'      : 1 },
          ],
        }, {
          'featureType': 'road.local',
          'stylers': [
            { 'hue'        : '#FF0300' },
            { 'saturation' : -100 },
            { 'lightness'  : 52 },
            { 'gamma'      : 1 },
          ],
        }, {
          'featureType': 'water',
          'stylers': [
            { 'hue'        : '#0078FF' },
            { 'saturation' : -13.200000000000003 },
            { 'lightness'  : 2.4000000000000057 },
            { 'gamma'      : 1 },
          ],
        }, {
          'featureType': 'poi',
          'stylers': [
            { 'hue'        : '#00FF6A' },
            { 'saturation' : -1.0989010989011234 },
            { 'lightness'  : 11.200000000000017 },
            { 'gamma'      : 1 },
          ],
        }],
      };

      const map = new google.maps.Map(document.getElementById('google-map'), mapOptions);

      new google.maps.Marker({
        map,
        position : new google.maps.LatLng(latitude, longitude),
        visible  : true,
      });
    });
  }
}())
