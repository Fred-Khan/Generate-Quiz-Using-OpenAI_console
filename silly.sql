SELECT 
quiz_prompts.prompt_text, 
quiz_questions.question_text, 
quiz_options.option_name, 
quiz_options.option_text, 
quiz_answers.answer_name
FROM 
(
    (
        quiz_prompts INNER JOIN quiz_questions ON quiz_prompts.id = quiz_questions.prompt_id
    ) 
    INNER JOIN quiz_options ON quiz_questions.id = quiz_options.id
) 
INNER JOIN quiz_answers ON quiz_questions.id = quiz_answers.id;
