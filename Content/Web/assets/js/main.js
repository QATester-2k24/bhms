function loaderShow(className){
    $('.'+className).css('visibility','visible');
    return true;
}

function loaderHide(className){
    $('.'+className).css('visibility','hidden');
    return true;
}

function showAlert(className, addClass, message){
    $('.'+className).css('display','block');
    $('.'+className).removeClass( $('.'+className).attr('data-class') );
    $('.'+className).attr('data-class', addClass);
    $('.'+className).addClass( addClass );
    $('.'+className+' > span').first().html( message );
    if(true){
        setTimeout(function(){
            hideAlert(className);
        }, 9000);
    }
    return true;
}

function hideAlert(className){
    $('.'+className).css('display','none');
    return true;
}

function showErrorAlert(className, addClass, message){
    $('.'+className).css('display','block');
    $('.'+className).removeClass( $('.'+className).attr('data-class') );
    $('.'+className).attr('data-class', addClass);
    $('.'+className).addClass( addClass );
    $('.'+className+' > div').first().html( message );
    if(true){
        setTimeout(function(){
            hideAlert(className);
        }, 9000);
    }
    return true;
}


$(document).ready(function(){

    $('.digitsOnly').on('keypress', function(e){
        var value = $(this).val();

        if (e.which != 8 && e.which != 0 && (e.which < 48 || e.which > 57)) { toastr["warning"]('Only Digits Allowed'); return false; }

    });

});