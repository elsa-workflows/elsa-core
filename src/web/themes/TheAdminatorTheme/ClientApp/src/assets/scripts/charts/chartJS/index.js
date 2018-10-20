import Chart from 'chart.js';
import { COLORS } from '../../constants/colors';

export default (function () {
  // ------------------------------------------------------
  // @Line Charts
  // ------------------------------------------------------

  const lineChartBox = document.getElementById('line-chart');

  if (lineChartBox) {
    const lineCtx = lineChartBox.getContext('2d');
    lineChartBox.height = 80;

    new Chart(lineCtx, {
      type: 'line',
      data: {
        labels: ['January', 'February', 'March', 'April', 'May', 'June', 'July'],
        datasets: [{
          label                : 'Series A',
          backgroundColor      : 'rgba(237, 231, 246, 0.5)',
          borderColor          : COLORS['deep-purple-500'],
          pointBackgroundColor : COLORS['deep-purple-700'],
          borderWidth          : 2,
          data                 : [60, 50, 70, 60, 50, 70, 60],
        }, {
          label                : 'Series B',
          backgroundColor      : 'rgba(232, 245, 233, 0.5)',
          borderColor          : COLORS['blue-500'],
          pointBackgroundColor : COLORS['blue-700'],
          borderWidth          : 2,
          data                 : [70, 75, 85, 70, 75, 85, 70],
        }],
      },

      options: {
        legend: {
          display: false,
        },
      },

    });
  }

  // ------------------------------------------------------
  // @Bar Charts
  // ------------------------------------------------------

  const barChartBox = document.getElementById('bar-chart');

  if (barChartBox) {
    const barCtx = barChartBox.getContext('2d');

    new Chart(barCtx, {
      type: 'bar',
      data: {
        labels: ['January', 'February', 'March', 'April', 'May', 'June', 'July'],
        datasets: [{
          label           : 'Dataset 1',
          backgroundColor : COLORS['deep-purple-500'],
          borderColor     : COLORS['deep-purple-800'],
          borderWidth     : 1,
          data            : [10, 50, 20, 40, 60, 30, 70],
        }, {
          label           : 'Dataset 2',
          backgroundColor : COLORS['light-blue-500'],
          borderColor     : COLORS['light-blue-800'],
          borderWidth     : 1,
          data            : [10, 50, 20, 40, 60, 30, 70],
        }],
      },

      options: {
        responsive: true,
        legend: {
          position: 'bottom',
        },
      },
    });
  }

  // ------------------------------------------------------
  // @Area Charts
  // ------------------------------------------------------

  const areaChartBox = document.getElementById('area-chart');

  if (areaChartBox) {
    const areaCtx = areaChartBox.getContext('2d');

    new Chart(areaCtx, {
      type: 'line',
      data: {
        labels: ['January', 'February', 'March', 'April', 'May', 'June', 'July'],
        datasets: [{
          backgroundColor : 'rgba(3, 169, 244, 0.5)',
          borderColor     : COLORS['light-blue-800'],
          data            : [10, 50, 20, 40, 60, 30, 70],
          label           : 'Dataset',
          fill            : 'start',
        }],
      },
    });
  }

  // ------------------------------------------------------
  // @Scatter Charts
  // ------------------------------------------------------

  const scatterChartBox = document.getElementById('scatter-chart');

  if (scatterChartBox) {
    const scatterCtx = scatterChartBox.getContext('2d');

    Chart.Scatter(scatterCtx, {
      data: {
        datasets: [{
          label           : 'My First dataset',
          borderColor     : COLORS['red-500'],
          backgroundColor : COLORS['red-500'],
          data: [
            { x: 10, y: 20 },
            { x: 30, y: 40 },
            { x: 50, y: 60 },
            { x: 70, y: 80 },
            { x: 90, y: 100 },
            { x: 110, y: 120 },
            { x: 130, y: 140 },
          ],
        }, {
          label           : 'My Second dataset',
          borderColor     : COLORS['green-500'],
          backgroundColor : COLORS['green-500'],
          data: [
            { x: 150, y: 160 },
            { x: 170, y: 180 },
            { x: 190, y: 200 },
            { x: 210, y: 220 },
            { x: 230, y: 240 },
            { x: 250, y: 260 },
            { x: 270, y: 280 },
          ],
        }],
      },
    });
  }
}())
