function RemovePattern() {
    var itemToRemove = $('#PatternsForCategory option:selected').text();
    var selectedCategory = $('#SelectedCategory option:selected').text();

    $.getJSON("/Category/RemovePattern?selectedCategory=" + selectedCategory + "&itemToRemove=" + itemToRemove);
    $("#PatternsForCategory option:selected").remove();
};

function AddPattern() {
    var newItem = $('#NewItem').val();
    var selectedCategory = $('#SelectedCategory option:selected').text();
    $.getJSON("/Category/AddPattern?selectedCategory=" + selectedCategory + "&newItem=" + newItem);
    $("#PatternsForCategory").append("<option>" + newItem + "</option>");
    $('#NewItem').text("");
};

$(function () {
    $("#AddPatternButton").click(AddPattern);
    $("#RemovePatternButton").click(RemovePattern);

    $('#SelectedCategory').change(function () {
        var selectedCategory = $(this).val();
        $.getJSON("/Category/GetPatterns?selectedCategory=" + selectedCategory, function (patterns) {
            $("#PatternsForCategory").empty();
            $("#PatternsForCategoryLabel").empty();

            $("#PatternsForCategoryLabel").text(selectedCategory);
            patterns.forEach(function (entry) {
                $("#PatternsForCategory").append("<option>" + entry + "</option>");
            });
        });
    });
});