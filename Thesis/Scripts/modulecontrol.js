var paper = Raphael("groundplan", 600, 600);
var a = paper.image("../Images/Vesalius_" + @ViewBag.image + "_2004-Model.svg", 0, 0, 600, 600);
var getpos;
var role;

$.ajax({
    url: '@Url.Action("GetRole", "Account")',
    type: 'GET',
    cache: false,
    success: function (data) {
        role = data;
    }
});

$.ajax({
    url: '@Url.Action("FetchAllSensors")',
    type: 'GET',
    data: {installationID: "@ViewBag.installation"},
    cache: false,
    datatype: 'json',
    success: function (data) {
        $.each(data, function (i, sensornode) {
            showSensor(sensornode);
        });
    }
});

function showSensor(sensornode) {
    var loc = sensornode.location.split(",");

    var c = paper.circle(loc[0], loc[1], 5);
    if (sensornode.inuse == true)
        c.attr({ fill: "green" });
    else c.attr({ fill: "red" });
    c.click(function () {
        $("#sensordetails").css("display", "inline-block");
        $("#sensordetails").html("<h2>Sensornode name: " + sensornode.name + "</h2><br>");
        $("#sensordetails").append("<h3>Sensornode ID: " + sensornode.id + "<\h3>");
        $("#sensordetails").append("<h3>Description: " + sensornode.description + "</h3><br>");
        var link = '@Url.Action("SensorDetails", "Sensor", new { sensorid = "-1", floorid = ViewBag.installation })';
        link = link.replace("-1", sensornode.id);
        var toAppend = '<input type="button" value="Advanced data" onclick="location.href="' + link + '"/>';
        $("#sensordetails").append("<h2>Sensors:</h2><br>");

        if(sensornode.sensors.length == 0) {
            $("#sensordetails").append("No sensors added to this node");
            if(role == "Installer")
                $("#sensordetails").append('<br><button onclick="createSensor(' + sensornode.id + ')">Create new sensor for this node</button><br>');
        }
        else {
            if(role == "Installer")
                $("#sensordetails").append('<button onclick="createSensor(' + sensornode.id + ')">Create new sensor for this node</button><br>');
            if (role == "Installer" || role == "Admin")
                $("#sensordetails").append('<br><a href="/Sensor/Sensordetails/' + @ViewBag.image + '?sensorgroupid=' + sensornode.id + '&floorid=' + @ViewBag.installation + '">Advanced sensor data for this node</a>');
            $.each(sensornode.sensors, function (i, sensor) {
                $("#sensordetails").append("<h3>Sensorname: " + sensor.name + "</h3><br>");
                $("#sensordetails").append("Sensor ID: " + sensor.id + "<br>");
                $("#sensordetails").append("Description: " + sensor.description + "<br>");
                $("#sensordetails").append("Frequency: " + sensor.frequency + "<br>");
                $("#sensordetails").append(sensor.type + ": " + sensor.lastvalue + "<br>");
                if (!(sensor.timestamp == ""))
                    $("#sensordetails").append("Timestamp: " + sensor.timestamp + "<br>");
                if (role == "Installer" || role == "Admin")
                    $("#sensordetails").append('<button onclick="promptForFrequency(' + sensornode.id + ', ' + sensor.id + ')">Change frequency</button><br>');
            });
        }
    });
}

function createSensorNode() {
    $('#createtypeform').css("display", "none");
    $("#createtypecontainer").css("z-index", "-1");
    $('#createsensorform').css("display", "none");
    $("#createsensorcontainer").css("z-index", "-1");
    alert("Please determine the position of the sensor by clicking on the groundplan", "Choose position");
    getpos = function (evt) {
        a.unclick(getpos);
        promptForSensorNodeValues(evt.pageX - $('#groundplan').offset().left, evt.pageY - $('#groundplan').offset().top);
    };
    a.click(getpos);
}

//TODO clean up of these ugly methods!
function createSensor(sensorgroupid) {
    $('#createtypeform').css("display", "none");
    $("#createtypecontainer").css("z-index", "-1");
    $('#createsensornodeform').css("display", "none");
    $("#createsensornodecontainer").css("z-index", "-1");
    $('#createsensorform').css("display", "block");
    $("#createsensorcontainer").css("z-index", "1000");
    $("#sensorgroupid").val(sensorgroupid);
}

function createType() {
    $("#typename").val("");
    $("#typefield").val("");
    $('#createsensorform').css("display", "none");
    $("#createsensorcontainer").css("z-index", "-1");
    $('#createsensornodeform').css("display", "none");
    $("#createsensornodecontainer").css("z-index", "-1");
    $("#createtypecontainer").css("z-index", "1000");
    $("#createtypeform").css("display", "block");
}


function promptForSensorNodeValues(x, y) {
    $("#xycoord").val(x + "," + y);
    $("#sensornodename").val("");
    $("#sensordescription").val("");
    $("#createsensornodecontainer").css("z-index", "1000");
    $("#createsensornodeform").css("display", "block");
}

function promptForFrequency(sensornodeid, sensorid) {
    $("#createtypeform").css("display", "none");
    $("#createtypecontainer").css("z-index", "-1");
    $("#createsensorform").css("display", "none");
    $("#createsensorcontainer").css("z-index", "-1");
    $("#createsensornodeform").css("display", "none");
    $("#createsensornodecontainer").css("z-index", "-1");
    $("#changefrequencycontainer").css("z-index", "1000");
    $("#changefrequencyform").css("display", "block");
    $("#sensornodeid").val(sensornodeid);
    $("#sensorid").val(sensorid);
}

function makeSensorNode(arg) {
    if("Cancel" == arg) {
        $("#createsensornodeform").css("display", "none");
        $("#createsensornodecontainer").css("z-index", "-1");
        return;
    }
    else {
        var form = document.getElementById("createsensornodeform");
        var sensornodename = form.name.value;
        var sensorlocation = form.location.value;
        var sensorzigbeeaddress = form.zigbeeaddress.value
        var sensordescription = form.description.value;
        $("#createsensornodeform").css("display", "none");
        $("#createsensornodecontainer").css("z-index", "-1");

        $.ajax({
            url: '@Url.Action("CreateSensorNode")',
            type: 'POST',
            data: { installationID: "@ViewBag.installation", name: sensornodename, location: sensorlocation, zigbeeAddress: sensorzigbeeaddress, description: sensordescription },
            cache: false,
            success: function () {
                alert("Sensornode made successfully!");
                location.reload();
            }
        });
    }
}

function makeSensor(arg) {
    if ("Cancel" == arg) {
        $("#createsensorform").css("display", "none");
        $("#createsensorcontainer").css("z-index", "-1");
        return;
    }
    else {
        var form = document.getElementById("createsensorform");
        var sensorgroupid = parseInt(form.sensorgroupid.value);
        var sensorname = form.name.value;
        var sensorfrequency = parseInt(form.frequency.value);
        var sensordescription = form.description.value;
        var sensordataname = form.sensortype.value;
        var sensortype = $("#sensortype option:selected").text();
        $("#createsensorform").css("display", "none");
        $("#createsensorcontainer").css("z-index", "-1");

        $.ajax({
            url: '@Url.Action("CreateSensor")',
            type: 'POST',
            data: { installationID: "@ViewBag.installation", sensorgroupID: sensorgroupid, name: sensorname, frequency: sensorfrequency, description: sensordescription, dataname: sensordataname, sensorType: sensortype },
            cache: false,
            success: function () {
                alert("Sensor made successfully!");
                location.reload();
            }
        });
    }
}

function makeType(arg) {
    if ("Cancel" == arg) {
        $("#createtypeform").css("display", "none");
        $("#createtypecontainer").css("z-index", "-1");
        return;
    }
    else {
        var form = document.getElementById("createtypeform");
        var typename = form.name.value;
        var typefield = form.field.value;
        $('#createtypeform').css("display", "none");
        $("#createtypecontainer").css("z-index", "-1");

        $.ajax({
            url: '@Url.Action("CreateSensorType")',
            type: 'POST',
            data: { name: typename, field: typefield },
            cache: false,
            success: function () {
                alert("Sensor type made successfully!");
                location.reload();
            }
        });
    }
}

function changeFrequency(arg) {
    if ("Cancel" == arg) {
        $("#changefrequencyform").css("display", "none");
        $("#changefrequencycontainer").css("z-index", "-1");
        return;
    }
    else {
        var form = document.getElementById("changefrequencyform");
        var nodeid = parseInt(form.sensornodeid.value);
        var id = parseInt(form.sensorid.value);
        var newfrequency = parseInt(form.frequency.value);
        $("#changefrequencyform").css("display", "none");
        $("#changefrequencycontainer").css("z-index", "-1");

        $.ajax({
            url: '@Url.Action("ChangeFrequency")',
            type: 'POST',
            data: { sensorGroupID: nodeid, sensorID: id, newFrequency: newfrequency },
            cache: false,
            success: function () {
                alert("Frequency successfully changed!");
                location.reload();
            }
        });
    }
}