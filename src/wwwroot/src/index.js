// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
import { UserAutocomplete } from './js/userAutocomplete.js'
import { initAll } from "govuk-frontend";
initAll();
import MOJFrontend from "@ministryofjustice/frontend/moj/all";
MOJFrontend.initAll();


//Temporary solution until the TagHelper Library provides a Date component that only accepts a month and year
function setAndDisableSignificantDateDayField() {
    
    const significantDateProp = $("#SignificantDate\\.Day");

    significantDateProp.val(1);
    significantDateProp.closest(".govuk-date-input__item").hide();
}

setAndDisableSignificantDateDayField();

// check for the appropriate element and progressively enhance to a autocomplete when
// found
const assignToTarget = document.getElementById('assignment-form-group')
if (assignToTarget) {
    const autocomplete = new UserAutocomplete()
    autocomplete.init(
        assignToTarget.id,
        'email',
        'Assign to'
    )
}

const addedByTarget = document.getElementById('added-by-form-group')
if (addedByTarget) {
    const autocomplete = new UserAutocomplete()
    autocomplete.init(
        addedByTarget.id,
        'email',
        'Added by'
    )
}

// set the js-enabled class on the body if JS is enabled
document.body.className = ((document.body.className) ? document.body.className + ' js-enabled' : 'js-enabled')
