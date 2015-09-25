function getParam(val) {
    var result = "Not found", tmp;

    var items = location.search.substr(1).split("&");
    for (var index = 0; index < items.length; index++) {
        tmp = items[index].split("=");
        if (tmp[0] === val) result = decodeURIComponent(tmp[1]);
    }

    return result;
}

function ShowData(entries, containerId, title, category) {
    var data = [];
    var categories = [];
    var total = 0;
    for (var i = 0; i < entries.length; i++) {
        data.push(
            {
                name: entries[i].Unit,
                y: entries[i].Sum
            }
        );
        total += entries[i].Sum;
        categories.push(entries[i].Unit);
    }

    var average = total / categories.length;
    title += " (Total: " + total.toFixed(2) + ", Average: " + average.toFixed(2) + ")";
    
    $(containerId).highcharts({
        chart: {
            zoomType: 'x',
            height: 300,
            panning: true,
            panKey: 'shift'
        },
        title: {
            text: title,
            x: -20 //center
        },
        yAxis: {
            min: 0,
            title: {
                text: 'CHF'
            },
            plotLines: [
                {
                    value: 0,
                    width: 1,
                    color: '#808080'
                },
                {
                    color: 'red',
                    value: average.toFixed(2),
                    width: '1',
                    zIndex: 2,
                    label: {
                        text: 'Avg:' + average.toFixed(2),
                        align: 'left',
                        y: -10,
                        x: 0,
                        style: {
                            color: 'red'
                        }
                    }
                }
            ]
        },
        credits: {
            enabled: false
        },
        xAxis: {
            categories: categories,
            tickInterval: 1
        },
        loading: {
            showDuration: 100,
            hideDuration: 100
        },
        legend: {
            enabled: false,
            layout: 'horizontal',
            align: 'bottom',
            verticalAlign: 'middle',
            borderWidth: 0
        },
        tooltip:
        {
            shape: "callout",
            format: '{point.y:,.2f}',
            crosshairs: {
                width: 1,
                color: '#D8EBF9'
            }
        },
        series: [
            {
                name: 'CHF',
                data: data,
               
                dataLabels: {
                    enabled: true,
                    format: '{point.y:,.2f}'
                },
                yAxis: 0,
                point: {
                    events: {
                        click: function () {
                            var monthName = this.category; // = month
                            var url = "/Home/GetListForCategoryAndMonth?category=" + category + "&month=" + (this.x+1);

                            $.getJSON(url, function (entries) {
                                $("#result").empty();
                                $("#resultheader").empty();
                                $("#resultheader").append("<h4>" + category + " / " + monthName + "</h4>");
                                $("#result").append("<thead style='background-color: #428bca;color: white;'><tr> <th>Date</th> <th>Description</th> <th style='text-align:right'>Amount</th> </tr></thead>");
                                $("#result").append("<tbody>");
                                
                                entries.forEach(function (entry) {
                                    var re = /-?\d+/;
                                    var m = re.exec(entry.Date);
                                    var d = new Date(parseInt(m[0]));
                                    var dateString =
                                        ("0" + d.getDate()).slice(-2)+ "." +
                                        ("0" + (d.getMonth() + 1)).slice(-2) + "." +
                                         d.getFullYear();
                                    $("#result").append("<tr> <td>" + dateString + "</td> <td>" + entry.Text + "</td> <td style='text-align:right'>" + entry.Amount.toFixed(2) + "</td> </tr>");
                                });
                                $("#result").append("</tbody></table>");
                            });
                        }
                    }
                }
            }
        ]
    });
}


$(function () {

    Highcharts.setOptions({
        lang: {
            decimalPoint: ".",
            thousandsSep: "'"
        }
    });

    var graphCategory = getParam('Category');
   
    $.getJSON("/Home/GetPatterns", function (patterns) {
        var totalOut = 0;
        patterns.forEach(function (entry) {
            if (entry.Category !== "Gutschriften" && entry.Category !== "Sparen" && entry.Category !== "Ignore" && entry.Category !== "OnceOnly") {
                totalOut += entry.Sum;
            } 
        });

        patterns.forEach(function (entry) {
            var percent = (entry.Sum / totalOut * 100).toFixed(0);
            if (entry.Category === "Gutschriften" || entry.Category === "Sparen" || entry.Category === "Ignore" || entry.Category === "OnceOnly") {
                percent = "---";
            }

            $("#allPatterns").append("<tbody>");
            if (entry.Category === graphCategory) {
                $("#allPatterns").append("<tr class='active'><td><a href='/Home/Index?Category=" + entry.Category + "'>"+entry.Category+"</a></td><td>"+entry.Count+"</td><td style='text-align:right'>"+entry.Sum.toFixed(2)+"</td><td>" + percent + "</td></tr>");
            } else {
                $("#allPatterns").append("<tr><td><a href='/Home/Index?Category=" + entry.Category + "'>" + entry.Category + "</a></td><td>" + entry.Count + "</td><td style='text-align:right'>" + entry.Sum.toFixed(2) + "</td><td>" + percent + "</td></tr>");
            }
        });

        $("#allPatterns").append("<tr><td><b>Total</b></td><td></td><td style='text-align:right'><b>" + totalOut.toFixed(2) + "</b></td><td></td></tr>");
        $("#allPatterns").append("</tbody>");
    });

    if (graphCategory !== "Not found") {
        var url = "/Home/GetOverviewPerMonth?category=" + graphCategory;

        $.getJSON(url, function(entries) {
            ShowData(entries, '#overviewGraph', "Monthly overview category: " + graphCategory, graphCategory);
        });
    } else {
        $("#overviewGraph").append("<div width='100px' class='panel panel-primary'><div class='panel-heading'>No category selected</div><div class='panel-body'><p>Please select a category.</p></div>");
        $("#overviewGraph").innerWidth("300px");
        $("#overviewGraph").css('margin-left', '100px');
    }
});